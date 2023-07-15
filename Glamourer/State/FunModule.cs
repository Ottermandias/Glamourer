using System;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Glamourer.Customization;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeIndex = Glamourer.Customization.CustomizeIndex;

namespace Glamourer.State;

public unsafe class FunModule
{
    private readonly ItemManager          _items;
    private readonly CustomizationService _customizations;
    private readonly CodeService          _codes;
    private readonly Random               _rng;
    private readonly StainId[]            _stains;

    public FunModule(CodeService codes, CustomizationService customizations, ItemManager items)
    {
        _codes          = codes;
        _customizations = customizations;
        _items          = items;
        _rng            = new Random();
        _stains         = _items.Stains.Keys.Prepend((StainId)0).ToArray();
    }

    public void ApplyFun(Actor actor, ref CharacterArmor armor, EquipSlot slot)
    {
        if (actor.AsObject->ObjectKind is not (byte)ObjectKind.Player || !actor.IsCharacter)
            return;

        if (actor.AsCharacter->CharacterData.ModelCharaId != 0)
            return;

        ApplyEmperor(new Span<CharacterArmor>(ref armor));
        ApplyClown(new Span<CharacterArmor>(ref armor));
    }

    public void ApplyFun(Actor actor, Span<CharacterArmor> armor, ref Customize customize)
    {
        if (actor.AsObject->ObjectKind is not (byte)ObjectKind.Player || !actor.IsCharacter)
            return;

        if (actor.AsCharacter->CharacterData.ModelCharaId != 0)
            return;

        ApplyEmperor(armor);
        ApplyClown(armor);

        ApplyOops(ref customize);
        ApplyIndividual(ref customize);
        ApplySizing(actor, ref customize);
    }

    public void ApplyClown(Span<CharacterArmor> armors)
    {
        if (!_codes.EnabledClown)
            return;

        foreach (ref var armor in armors)
        {
            var stainIdx = _rng.Next(0, _stains.Length - 1);
            armor.Stain = _stains[stainIdx];
        }
    }

    public void ApplyEmperor(Span<CharacterArmor> armors, EquipSlot slot = EquipSlot.Unknown)
    {
        if (!_codes.EnabledEmperor)
            return;

        void SetItem(EquipSlot slot2, ref CharacterArmor armor)
        {
            var list = _items.ItemService.AwaitedService[slot2.ToEquipType()];
            var rng  = _rng.Next(0, list.Count - 1);
            var item = list[rng];
            armor.Set     = item.ModelId;
            armor.Variant = item.Variant;
        }

        if (armors.Length == 1)
            SetItem(slot, ref armors[0]);
        else
            for (var i = 0u; i < armors.Length; ++i)
                SetItem(i.ToEquipSlot(), ref armors[(int)i]);
    }

    public void ApplyOops(ref Customize customize)
    {
        if (_codes.EnabledOops == Race.Unknown)
            return;

        var targetClan = (SubRace)((int)_codes.EnabledOops * 2 - (int)customize.Clan % 2);
        // TODO Female Hrothgar
        if (_codes.EnabledOops is Race.Hrothgar && customize.Gender is Gender.Female)
            targetClan = targetClan is SubRace.Lost ? SubRace.Seawolf : SubRace.Hellsguard;
        _customizations.ChangeClan(ref customize, targetClan);
    }

    public void ApplyIndividual(ref Customize customize)
    {
        if (!_codes.EnabledIndividual)
            return;

        var set = _customizations.AwaitedService.GetList(customize.Clan, customize.Gender);
        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            if (index is CustomizeIndex.Face || !set.IsAvailable(index))
                continue;

            var valueIdx = _rng.Next(0, set.Count(index) - 1);
            customize[index] = set.Data(index, valueIdx).Value;
        }
    }

    public void ApplySizing(Actor actor, ref Customize customize)
    {
        if (_codes.EnabledSizing == CodeService.Sizing.None)
            return;

        var size = _codes.EnabledSizing switch
        {
            CodeService.Sizing.Dwarf when actor.Index == 0 => 0,
            CodeService.Sizing.Dwarf when actor.Index != 0 => 100,
            CodeService.Sizing.Giant when actor.Index == 0 => 100,
            CodeService.Sizing.Giant when actor.Index != 0 => 0,
            _                                              => 0,
        };

        if (customize.Gender is Gender.Female)
            customize[CustomizeIndex.BustSize] = (CustomizeValue)size;
        customize[CustomizeIndex.Height] = (CustomizeValue)size;
    }
}
