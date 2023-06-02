using System;
using System.Collections;
using Glamourer.Interop;
using Penumbra.GameData.Actors;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Api;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Services;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;
using Object = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object;

namespace Glamourer.State;

public sealed partial class ActiveDesign
{
    [Flags]
    public enum ChangeType
    {
        Default = 0x00,
        Changed = 0x01,
        Fixed   = 0x02,
    }

    public class Manager : IReadOnlyDictionary<ActorIdentifier, ActiveDesign>
    {
        private readonly ActorService           _actors;
        private readonly ObjectManager          _objects;
        private readonly PenumbraAttach         _penumbra;
        private readonly ItemManager            _items;
        private readonly VisorService           _visor;
        private readonly ChangeCustomizeService _customize;
        private readonly UpdateSlotService      _updateSlot;
        private readonly WeaponService          _weaponService;

        private readonly Dictionary<ActorIdentifier, ActiveDesign> _characterSaves = new();

        public Manager(ActorService actors, ObjectManager objects, PenumbraAttach penumbra, ItemManager items, VisorService visor,
            ChangeCustomizeService customize, UpdateSlotService updateSlot, WeaponService weaponService)
        {
            _actors        = actors;
            _objects       = objects;
            _penumbra      = penumbra;
            _items         = items;
            _visor         = visor;
            _customize     = customize;
            _updateSlot    = updateSlot;
            _weaponService = weaponService;
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
            var id = _actors.AwaitedService.FromObject((GameObject*)actor.Pointer, out _, false, false, false);
            if (_characterSaves.TryGetValue(id, out var save))
            {
                save.Initialize(_items, actor);
                return save;
            }

            id   = id.CreatePermanent();
            save = new ActiveDesign(_items, id, actor);
            save.Initialize(_items, actor);
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
                ChangeMainHand(to, from.MainHandId, fromFixed);
            if (from.DoApplyStain(EquipSlot.MainHand))
                ChangeStain(to, EquipSlot.MainHand, from.WeaponMain.Stain, fromFixed);

            if (from.DoApplyEquip(EquipSlot.OffHand))
                ChangeOffHand(to, from.OffHandId, fromFixed);
            if (from.DoApplyStain(EquipSlot.OffHand))
                ChangeStain(to, EquipSlot.OffHand, from.WeaponOff.Stain, fromFixed);

            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                var armor = from.Armor(slot);
                if (from.DoApplyEquip(slot))
                    ChangeEquipment(to, slot, armor, fromFixed);
                if (from.DoApplyStain(slot))
                    ChangeStain(to, slot, armor.Stain, fromFixed);
            }

            ChangeCustomize(to, from.ApplyCustomize, from.ModelData.Customize.Data, fromFixed);

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
            => design.SetMainhand(_items, itemId);

        public void ChangeOffHand(ActiveDesign design, uint itemId, bool fromFixed)
            => design.SetOffhand(_items, itemId);

        public void RevertMainHand(ActiveDesign design)
        { }

        public void RevertOffHand(ActiveDesign design)
        { }

        public void RevertCustomize(ActiveDesign design, CustomizeFlag flags)
            => ChangeCustomize(design, flags, design._initialData.Customize.Data, false);

        public void ChangeCustomize(ActiveDesign design, CustomizeFlag flags, CustomizeData newValue, bool fromFixed)
        {
            var customize  = new Customize(newValue);
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
                    _customize.UpdateCustomize(obj, design.ModelData.Customize.Data);
            }
        }

        public void RevertEquipment(ActiveDesign design, EquipSlot slot, bool equip, bool stain)
        {
            var item = design._initialData.Armor(slot);
            if (equip)
            {
                var flag = slot.ToFlag();
                design.UpdateArmor(_items, slot, item, true);
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
                _updateSlot.UpdateSlot(obj.DrawObject, slot, item);
        }

        public void ChangeEquipment(ActiveDesign design, EquipSlot slot, Item item, bool fromFixed)
        {
            var flag = slot.ToFlag();
            design.SetArmor(slot, item);
            var current = design.Armor(slot);
            var initial = design._initialData.Armor(slot);
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
                _updateSlot.UpdateSlot(obj.DrawObject, slot, item.Model);
        }

        public void ChangeStain(ActiveDesign design, EquipSlot slot, StainId stain, bool fromFixed)
        {
            var flag = slot.ToStainFlag();
            design.SetStain(slot, stain);
            var (current, initial, weapon) = slot switch
            {
                EquipSlot.MainHand => (design.WeaponMain.Stain, design._initialData.MainHand.Stain, true),
                EquipSlot.OffHand  => (design.WeaponOff.Stain, design._initialData.OffHand.Stain, true),
                _                  => (design.Armor(slot).Stain, design._initialData.Armor(slot).Stain, false),
            };
            if (current.Value != initial.Value)
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
            {
                if (weapon)
                    _weaponService.LoadStain(obj, EquipSlot.MainHand, stain);
                else
                    _updateSlot.UpdateStain(obj.DrawObject, slot, stain);
            }
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
                _visor.SetVisorState(obj.DrawObject, on);
        }
    }
}
