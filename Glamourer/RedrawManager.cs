using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Penumbra.GameData.ByteString;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer;

public unsafe class RedrawManager : IDisposable
{
    public delegate ulong FlagSlotForUpdateDelegate(Human* drawObject, uint slot, CharacterArmor* data);

    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A")]
    public Hook<FlagSlotForUpdateDelegate> FlagSlotForUpdateHook = null!;

    private ulong FlagSlotForUpdateDetour(Human* drawObject, uint slot, CharacterArmor* data)
    {
        return FlagSlotForUpdateHook.Original(drawObject, slot, data);
    }

    public delegate void LoadWeaponDelegate(IntPtr characterOffset, uint slot, ulong data, byte unk);

    [Signature("E8 ?? ?? ?? ?? 44 8B 9F")]
    public Hook<LoadWeaponDelegate> LoadWeaponHook = null!;

    private void LoadWeaponDetour(IntPtr characterOffset, uint slot, ulong data, byte unk)
    {
        const int offset = 0xD8 * 8;
        PluginLog.Information($"0x{characterOffset:X}, 0x{characterOffset - offset:X}, {slot}, {data:16X}, {unk}");
        LoadWeaponHook.Original(characterOffset, slot, data, unk);
    }

    //[Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A",
    //    DetourName = nameof(FlagSlotForUpdateDetour))]
    //public Hook<FlagSlotForUpdateDelegate>? FlagSlotForUpdateHook;
    //
    //    public readonly FixedDesigns FixedDesigns;
    //

    private readonly Dictionary<string, CharacterSave> _currentRedraws = new(32);

    public RedrawManager()
    {
        SignatureHelper.Initialise(this);
//        FixedDesigns = new FixedDesigns(designs);
        Glamourer.Penumbra.CreatingCharacterBase += OnCharacterRedraw;
        FlagSlotForUpdateHook.Enable();
        LoadWeaponHook.Enable();
//
//        if (Glamourer.Config.ApplyFixedDesigns)
//            Enable();
    }

    public void Dispose()
    {
        FlagSlotForUpdateHook.Dispose();
        LoadWeaponHook.Dispose();
        Glamourer.Penumbra.CreatingCharacterBase -= OnCharacterRedraw;
        //FlagSlotForUpdateHook?.Dispose();
    }

    public void Set(Character* actor, CharacterSave save)
    {
        var name = GetName(actor);
        if (name.Length == 0)
            return;

        _currentRedraws[name] = save;
    }

    public void Set(IntPtr actor, CharacterSave save)
        => Set((Character*)actor, save);

    public void Revert(Character* actor)
        => _currentRedraws.Remove(GetName(actor));

    public void Revert(IntPtr actor)
        => Revert((Character*)actor);

    private static string GetName(Character* actor)
    {
        return string.Concat(new Utf8String(actor->GameObject.Name)
            .Select(c => (char)c)
            .Append(actor->GameObject.ObjectKind == (byte)ObjectKind.Pc ? (char)actor->HomeWorld : (char)actor->GameObject.ObjectIndex));
    }

    private void Cleanup(object? _, ushort _1)
        => _currentRedraws.Clear();

    public void ChangeEquip(Human* actor, EquipSlot slot, CharacterArmor item)
        => Flag(actor, slot.ToIndex(), &item);

    public void ChangeEquip(Character* character, EquipSlot slot, CharacterArmor item)
        => ChangeEquip((Human*)character->GameObject.DrawObject, slot, item);

    public void ChangeEquip(IntPtr character, EquipSlot slot, CharacterArmor item)
        => ChangeEquip((Character*)character, slot, item);

    private void OnCharacterRedraw(IntPtr addr, IntPtr modelId, IntPtr customize, IntPtr equipData)
    {
        var name = GetName((Character*)addr);
        if (_currentRedraws.TryGetValue(name, out var save))
        {
            *(CustomizationData*)customize = *(CustomizationData*)save.Customize.Address;
            var equip    = (CharacterEquip)equipData;
            var newEquip = save.Equipment;
            for (var i = 0; i < 10; ++i)
                equip[i] = newEquip[i];
        }


        //*(uint*)modelId = 0;

        //var human = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)addr;
        //if (human->GameObject.ObjectKind is (byte)ObjectKind.EventNpc or (byte)ObjectKind.BattleNpc or (byte)ObjectKind.Player
        // && human->ModelCharaId == 0)
        //{
        //    var name = new Utf8String(human->GameObject.Name).ToString();
        //    if (FixedDesigns.EnabledDesigns.TryGetValue(name, out var designs))
        //    {
        //        var design = designs.OrderBy(d => d.Jobs.Count).FirstOrDefault(d => d.Jobs.Fits(human->ClassJob));
        //        if (design != null)
        //        {
        //            if (design.Design.Data.WriteCustomizations)
        //                *(CharacterCustomization*)customize = design.Design.Data.Customizations;
        //
        //            var data = (uint*)equipData;
        //            for (var i = 0u; i < 10; ++i)
        //            {
        //                var slot = i.ToEquipSlot();
        //                if (design.Design.Data.WriteEquipment.Fits(slot))
        //                    data[i] = slot switch
        //                    {
        //                        EquipSlot.Head    => design.Design.Data.Equipment.Head.Value,
        //                        EquipSlot.Body    => design.Design.Data.Equipment.Body.Value,
        //                        EquipSlot.Hands   => design.Design.Data.Equipment.Hands.Value,
        //                        EquipSlot.Legs    => design.Design.Data.Equipment.Legs.Value,
        //                        EquipSlot.Feet    => design.Design.Data.Equipment.Feet.Value,
        //                        EquipSlot.Ears    => design.Design.Data.Equipment.Ears.Value,
        //                        EquipSlot.Neck    => design.Design.Data.Equipment.Neck.Value,
        //                        EquipSlot.Wrists  => design.Design.Data.Equipment.Wrists.Value,
        //                        EquipSlot.RFinger => design.Design.Data.Equipment.RFinger.Value,
        //                        EquipSlot.LFinger => design.Design.Data.Equipment.LFinger.Value,
        //                        _                 => 0,
        //                    };
        //            }
        //        }
        //    }
        //}
    }
//
//    private ulong FlagSlotForUpdateDetour(Human* drawObject, uint slotIdx, uint* data)
//    {
//        ulong ret;
//        var   slot = slotIdx.ToEquipSlot();
//        try
//        {
//            if (slot != EquipSlot.Unknown)
//            {
//                var gameObject =
//                    (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)Glamourer.Penumbra.GameObjectFromDrawObject((IntPtr)drawObject);
//                if (gameObject != null)
//                {
//                    var name = new Utf8String(gameObject->GameObject.Name).ToString();
//                    if (FixedDesigns.EnabledDesigns.TryGetValue(name, out var designs))
//                    {
//                        var design = designs.OrderBy(d => d.Jobs.Count).FirstOrDefault(d => d.Jobs.Fits(gameObject->ClassJob));
//                        if (design != null && design.Design.Data.WriteEquipment.Fits(slot))
//                            *data = slot switch
//                            {
//                                EquipSlot.Head    => design.Design.Data.Equipment.Head.Value,
//                                EquipSlot.Body    => design.Design.Data.Equipment.Body.Value,
//                                EquipSlot.Hands   => design.Design.Data.Equipment.Hands.Value,
//                                EquipSlot.Legs    => design.Design.Data.Equipment.Legs.Value,
//                                EquipSlot.Feet    => design.Design.Data.Equipment.Feet.Value,
//                                EquipSlot.Ears    => design.Design.Data.Equipment.Ears.Value,
//                                EquipSlot.Neck    => design.Design.Data.Equipment.Neck.Value,
//                                EquipSlot.Wrists  => design.Design.Data.Equipment.Wrists.Value,
//                                EquipSlot.RFinger => design.Design.Data.Equipment.RFinger.Value,
//                                EquipSlot.LFinger => design.Design.Data.Equipment.LFinger.Value,
//                                _                 => 0,
//                            };
//                    }
//                }
//            }
//        }
//        finally
//        {
//            ret = FlagSlotForUpdateHook!.Original(drawObject, slotIdx, data);
//        }
//
//        return ret;
//    }
//
//    public void UpdateSlot(Human* drawObject, EquipSlot slot, CharacterArmor data)
//    {
//        var idx = slot.ToIndex();
//        if (idx >= 10)
//            return;
//
//        FlagSlotForUpdateDetour(drawObject, idx, (uint*)&data);
//    }
}
