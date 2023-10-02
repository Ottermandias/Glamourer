using System;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Gui;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using ImGuiNET;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeIndex = Glamourer.Customization.CustomizeIndex;

namespace Glamourer.State;

public unsafe class FunModule : IDisposable
{
    public enum FestivalType
    {
        None,
        Halloween,
        Christmas,
        AprilFirst,
    }

    private readonly WorldSets            _worldSets = new();
    private readonly ItemManager          _items;
    private readonly CustomizationService _customizations;
    private readonly Configuration        _config;
    private readonly CodeService          _codes;
    private readonly Random               _rng;
    private readonly GenericPopupWindow   _popupWindow;
    private readonly StateManager         _stateManager;
    private readonly DesignConverter      _designConverter;
    private readonly DesignManager        _designManager;
    private readonly ObjectManager        _objects;
    private readonly StainId[]            _stains;

    public  FestivalType CurrentFestival { get; private set; } = FestivalType.None;
    private FunEquipSet? _festivalSet;

    private void OnDayChange(int day, int month, int _)
    {
        CurrentFestival = (day, month) switch
        {
            (1, 4)   => FestivalType.AprilFirst,
            (24, 12) => FestivalType.Christmas,
            (25, 12) => FestivalType.Christmas,
            (26, 12) => FestivalType.Christmas,
            (31, 10) => FestivalType.Halloween,
            (01, 11) => FestivalType.Halloween,
            _        => FestivalType.None,
        };
        _festivalSet                   = FunEquipSet.GetSet(CurrentFestival);
        _popupWindow.OpenFestivalPopup = _festivalSet != null && _config.DisableFestivals == 1;
    }

    internal void ForceFestival(FestivalType type)
    {
        CurrentFestival                = type;
        _festivalSet                   = FunEquipSet.GetSet(CurrentFestival);
        _popupWindow.OpenFestivalPopup = _festivalSet != null && _config.DisableFestivals == 1;
    }

    internal void ResetFestival()
        => OnDayChange(DateTime.UtcNow.Day, DateTime.UtcNow.Month, DateTime.UtcNow.Year);

    public FunModule(CodeService codes, CustomizationService customizations, ItemManager items, Configuration config,
        GenericPopupWindow popupWindow, StateManager stateManager, ObjectManager objects, DesignConverter designConverter,
        DesignManager designManager)
    {
        _codes           = codes;
        _customizations  = customizations;
        _items           = items;
        _config          = config;
        _popupWindow     = popupWindow;
        _stateManager    = stateManager;
        _objects         = objects;
        _designConverter = designConverter;
        _designManager   = designManager;
        _rng             = new Random();
        _stains          = _items.Stains.Keys.Prepend((StainId)0).ToArray();
        ResetFestival();
        DayChangeTracker.DayChanged += OnDayChange;
    }

    public void Dispose()
        => DayChangeTracker.DayChanged -= OnDayChange;

    public void ApplyFun(Actor actor, ref CharacterArmor armor, EquipSlot slot)
    {
        if (actor.AsObject->ObjectKind is not (byte)ObjectKind.Player || !actor.IsCharacter)
            return;

        if (actor.AsCharacter->CharacterData.ModelCharaId != 0)
            return;

        ApplyEmperor(new Span<CharacterArmor>(ref armor), slot);
        ApplyClown(new Span<CharacterArmor>(ref armor));
    }

    public void ApplyFun(Actor actor, Span<CharacterArmor> armor, ref Customize customize)
    {
        if (actor.AsObject->ObjectKind is not (byte)ObjectKind.Player || !actor.IsCharacter)
            return;

        if (actor.AsCharacter->CharacterData.ModelCharaId != 0)
            return;

        if (_config.DisableFestivals == 0 && _festivalSet != null)
        {
            _festivalSet.Apply(_stains, _rng, armor);
        }
        else if (_codes.EnabledWorld && actor.Index != 0)
        {
            _worldSets.Apply(actor, _rng, armor);
        }
        else
        {
            ApplyEmperor(armor);
            ApplyClown(armor);
        }

        ApplyOops(ref customize);
        Apply63(ref customize);
        ApplyIndividual(ref customize);
        ApplySizing(actor, ref customize);
    }

    public void ApplyFun(Actor actor, ref CharacterWeapon weapon, EquipSlot slot)
    {
        if (actor.AsObject->ObjectKind is not (byte)ObjectKind.Player || !actor.IsCharacter)
            return;

        if (actor.AsCharacter->CharacterData.ModelCharaId != 0)
            return;

        if (_codes.EnabledWorld)
            _worldSets.Apply(actor, _rng, ref weapon, slot);
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

    public void Apply63(ref Customize customize)
    {
        if (!_codes.Enabled63 || customize.Race is Race.Hrothgar) // TODO Female Hrothgar
            return;

        _customizations.ChangeGender(ref customize, customize.Gender is Gender.Male ? Gender.Female : Gender.Male);
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

    public void WhoAmI()
        => WhoIsThat(_objects.Player);

    public void WhoIsThat()
        => WhoIsThat(_objects.Target);

    private void WhoIsThat(Actor actor)
    {
        if (!actor.IsCharacter)
            return;

        try
        {
            var tmp = _designManager.CreateTemporary();
            tmp.DesignData = _stateManager.FromActor(actor, true);
            tmp.FixCustomizeApplication(_customizations, CustomizeFlagExtensions.AllRelevant);
            var data = _designConverter.ShareBase64(tmp);
            ImGui.SetClipboardText(data);
            Glamourer.Chat.NotificationMessage($"Copied current actual design of {actor.Utf8Name} to clipboard.");
        }
        catch
        {
            // ignored
        }
    }
}
