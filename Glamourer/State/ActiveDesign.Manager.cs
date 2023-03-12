using System;
using System.Collections;
using Glamourer.Interop;
using Penumbra.GameData.Actors;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Api;
using Glamourer.Customization;
using Glamourer.Designs;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;
using Item = Glamourer.Designs.Item;
using Lumina.Excel.GeneratedSheets;

namespace Glamourer.State;

public sealed partial class ActiveDesign
{
    public partial class Manager : IReadOnlyDictionary<ActorIdentifier, ActiveDesign>
    {
        private readonly ActorManager    _actors;
        private readonly ObjectManager   _objects;
        private readonly Interop.Interop _interop;
        private readonly PenumbraAttach  _penumbra;

        private readonly Dictionary<ActorIdentifier, ActiveDesign> _characterSaves = new();

        public Manager(ActorManager actors, ObjectManager objects, Interop.Interop interop, PenumbraAttach penumbra)
        {
            _actors   = actors;
            _objects  = objects;
            _interop  = interop;
            _penumbra = penumbra;
        }

        public IEnumerator<KeyValuePair<ActorIdentifier, ActiveDesign>> GetEnumerator()
            => _characterSaves.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public int Count
            => _characterSaves.Count;

        public bool ContainsKey(ActorIdentifier key)
            => _characterSaves.ContainsKey(key);

        public bool TryGetValue(ActorIdentifier key, [NotNullWhen(true)] out ActiveDesign? value)
            => _characterSaves.TryGetValue(key, out value);

        public ActiveDesign this[ActorIdentifier key]
            => _characterSaves[key];

        public IEnumerable<ActorIdentifier> Keys
            => _characterSaves.Keys;

        public IEnumerable<ActiveDesign> Values
            => _characterSaves.Values;

        public void DeleteSave(ActorIdentifier identifier)
            => _characterSaves.Remove(identifier);

        public unsafe ActiveDesign GetOrCreateSave(Actor actor)
        {
            var id = _actors.FromObject((GameObject*)actor.Pointer, out _, false, false, false);
            if (_characterSaves.TryGetValue(id, out var save))
            {
                save.Initialize(actor);
                return save;
            }

            id   = id.CreatePermanent();
            save = new ActiveDesign(id, actor);
            save.Initialize(actor);
            _characterSaves.Add(id, save);
            return save;
        }

        public void SetWetness(ActiveDesign design, bool wet, bool fromFixed)
            => design.IsWet = wet;

        public void SetHatVisible(ActiveDesign design, bool visible, bool fromFixed)
            => design.IsHatVisible = visible;

        public void SetVisor(ActiveDesign design, bool toggled, bool fromFixed)
            => design.IsVisorToggled = toggled;

        public void SetWeaponVisible(ActiveDesign design, bool visible, bool fromFixed)
            => design.IsWeaponVisible = visible;

        public unsafe void ApplyDesign(ActiveDesign to, Design from, bool fromFixed)
        {
            if (to.ModelId != from.ModelId)
                return;

            if (from.DoApplyEquip(EquipSlot.MainHand))
                ChangeMainHand(to, from.MainHand, fromFixed);
                    
            if (from.DoApplyEquip(EquipSlot.OffHand))
                ChangeOffHand(to, from.OffHand, fromFixed);

            foreach (var slot in EquipSlotExtensions.EqdpSlots.Where(from.DoApplyEquip))
                ChangeEquipment(to, slot, from.Armor(slot), fromFixed);

            ChangeCustomize(to, from.ApplyCustomize, *from.Customize().Data, fromFixed);

            if (from.Wetness.Enabled)
                SetWetness(to, from.Wetness.ForcedValue, fromFixed);
            if (from.Hat.Enabled)
                SetHatVisible(to, from.Hat.ForcedValue, fromFixed);
            if (from.Visor.Enabled)
                SetVisor(to, from.Visor.ForcedValue, fromFixed);
            if (from.Weapon.Enabled)
                SetWeaponVisible(to, from.Weapon.ForcedValue, fromFixed);
        }

        public void RevertDesign(ActiveDesign design)
        {
            RevertCustomize(design, design.ChangedCustomize);
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
                RevertEquipment(design, slot, design.ChangedEquip.HasFlag(slot.ToFlag()), design.ChangedEquip.HasFlag(slot.ToStainFlag()));

            RevertMainHand(design);
            RevertOffHand(design);
        }

        public void ChangeMainHand(ActiveDesign design, uint itemId, bool fromFixed)
            => design.SetMainhand(itemId);

        public void ChangeOffHand(ActiveDesign design, uint itemId, bool fromFixed)
            => design.SetOffhand(itemId);

        public void RevertMainHand(ActiveDesign design)
        { }

        public void RevertOffHand(ActiveDesign design)
        { }

        public void RevertCustomize(ActiveDesign design, CustomizeFlag flags)
            => ChangeCustomize(design, flags, design._initialData.CustomizeData, false);

        public void ChangeCustomize(ActiveDesign design, CustomizeFlag flags, CustomizeData newValue, bool fromFixed)
        {
            var customize  = new Customize(ref newValue);
            var anyChanges = false;
            foreach (var option in Enum.GetValues<CustomizeIndex>())
            {
                var flag  = option.ToFlag();
                var apply = flags.HasFlag(flag);
                anyChanges |= apply && design.SetCustomize(option, customize[option]);
                if (design.GetCustomize(option).Value != design._initialData.Customize[option].Value)
                    design.ChangedCustomize |= flag;
                else
                    design.ChangedCustomize &= ~flag;

                if (fromFixed)
                    design.FixedCustomize |= flag;
                else
                    design.FixedCustomize &= ~flag;
            }

            if (!anyChanges)
                return;

            _objects.Update();
            if (!_objects.TryGetValue(design.Identifier, out var data))
                return;

            var redraw = flags.RequiresRedraw();
            foreach (var obj in data.Objects)
            {
                if (redraw)
                    _penumbra.RedrawObject(obj, RedrawType.Redraw);
                else
                    _interop.UpdateCustomize(obj, design.CharacterData.CustomizeData);
            }
        }

        public void RevertEquipment(ActiveDesign design, EquipSlot slot, bool equip, bool stain)
        {
            var item = design._initialData.Equipment[slot];
            if (equip)
            {
                var flag = slot.ToFlag();
                design.UpdateArmor(slot, item, true);
                design.ChangedEquip &= ~flag;
                design.FixedEquip   &= ~flag;
            }

            if (stain)
            {
                var flag = slot.ToStainFlag();
                design.SetStain(slot, item.Stain);
                design.ChangedEquip &= ~flag;
                design.FixedEquip   &= ~flag;
            }

            _objects.Update();
            if (!_objects.TryGetValue(design.Identifier, out var data))
                return;

            foreach (var obj in data.Objects)
                _interop.UpdateSlot(obj.DrawObject, slot, item);
        }

        public void ChangeEquipment(ActiveDesign design, EquipSlot slot, Item item, bool fromFixed)
        {
            var flag = slot.ToFlag();
            design.SetArmor(slot, item);
            var current = design.Armor(slot);
            var initial = design._initialData.Equipment[slot];
            if (current.ModelBase.Value != initial.Set.Value || current.Variant != initial.Variant)
                design.ChangedEquip |= flag;
            else
                design.ChangedEquip &= ~flag;
            if (fromFixed)
                design.FixedEquip |= flag;
            else
                design.FixedEquip &= ~flag;

            _objects.Update();
            if (!_objects.TryGetValue(design.Identifier, out var data))
                return;

            foreach (var obj in data.Objects)
                _interop.UpdateSlot(obj.DrawObject, slot, item.Model);
        }

        public void ChangeStain(ActiveDesign design, EquipSlot slot, StainId stain, bool fromFixed)
        {
            var flag = slot.ToStainFlag();
            design.SetStain(slot, stain);
            var current = design.Armor(slot);
            var initial = design._initialData.Equipment[slot];
            if (current.Stain.Value != initial.Stain.Value)
                design.ChangedEquip |= flag;
            else
                design.ChangedEquip &= ~flag;
            if (fromFixed)
                design.FixedEquip |= flag;
            else
                design.FixedEquip &= ~flag;

            _objects.Update();
            if (!_objects.TryGetValue(design.Identifier, out var data))
                return;

            foreach (var obj in data.Objects)
                _interop.UpdateStain(obj.DrawObject, slot, stain);
        }

        public void ChangeVisor(ActiveDesign design, bool on, bool fromFixed)
        {
            var current = design.IsVisorToggled;
            if (current == on)
                return;

            design.IsVisorToggled = on;
            _objects.Update();
            if (!_objects.TryGetValue(design.Identifier, out var data))
                return;

            foreach (var obj in data.Objects) 
                Interop.Interop.SetVisorState(obj.DrawObject, on);
        }
    }
}
