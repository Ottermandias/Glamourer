using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Designs;
using Glamourer.Gui;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using CustomizeIndex = Penumbra.GameData.Enums.CustomizeIndex;
using ObjectManager = Glamourer.Interop.ObjectManager;

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

    private readonly WorldSets          _worldSets = new();
    private readonly ItemManager        _items;
    private readonly CustomizeService   _customizations;
    private readonly Configuration      _config;
    private readonly CodeService        _codes;
    private readonly Random             _rng;
    private readonly GenericPopupWindow _popupWindow;
    private readonly StateManager       _stateManager;
    private readonly DesignConverter    _designConverter;
    private readonly DesignManager      _designManager;
    private readonly ObjectManager      _objects;
    private readonly StainId[]          _stains;

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
        => OnDayChange(DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year);

    public FunModule(CodeService codes, CustomizeService customizations, ItemManager items, Configuration config,
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

    private bool IsInFestival
        => _config.DisableFestivals == 0 && _festivalSet != null;

    public void ApplyFunToSlot(Actor actor, ref CharacterArmor armor, EquipSlot slot)
    {
        if (!ValidFunTarget(actor))
            return;

        if (IsInFestival)
        {
            KeepOldArmor(actor, slot, ref armor);
            return;
        }

        if (_codes.Enabled(CodeService.CodeFlag.Crown)
         && actor.OnlineStatus is OnlineStatus.PvEMentor or OnlineStatus.PvPMentor or OnlineStatus.TradeMentor
         && slot.IsEquipment())
        {
            armor = new CharacterArmor(6117, 1, 0);
            return;
        }

        switch (_codes.Masked(CodeService.GearCodes))
        {
            case CodeService.CodeFlag.Emperor:
                SetRandomItem(slot, ref armor);
                break;
            case CodeService.CodeFlag.Elephants:
            case CodeService.CodeFlag.Dolphins:
            case CodeService.CodeFlag.World when actor.Index != 0:
                KeepOldArmor(actor, slot, ref armor);
                break;
        }

        switch (_codes.Masked(CodeService.DyeCodes))
        {
            case CodeService.CodeFlag.Clown:
                SetRandomDye(ref armor);
                break;
        }
    }

    public void ApplyFunOnLoad(Actor actor, Span<CharacterArmor> armor, ref CustomizeArray customize)
    {
        if (!ValidFunTarget(actor))
            return;

        // First set the race, if any.
        SetRace(ref customize);
        // Now apply the gender.
        SetGender(ref customize);
        // Randomize customizations inside the race and gender combo.
        RandomizeCustomize(ref customize);
        // Finally, apply forced sizes.
        SetSize(actor, ref customize);

        // Apply the festival gear with priority over all gear codes.
        if (IsInFestival)
        {
            _festivalSet!.Apply(_stains, _rng, armor);
            return;
        }

        if (_codes.Enabled(CodeService.CodeFlag.Crown)
         && actor.OnlineStatus is OnlineStatus.Mentor or OnlineStatus.PvEMentor or OnlineStatus.PvPMentor or OnlineStatus.TradeMentor)
        {
            SetCrown(armor);
            return;
        }

        switch (_codes.Masked(CodeService.GearCodes))
        {
            case CodeService.CodeFlag.Emperor:
                foreach (var (slot, idx) in EquipSlotExtensions.EqdpSlots.WithIndex())
                    SetRandomItem(slot, ref armor[idx]);
                break;
            case CodeService.CodeFlag.Elephants:
                var stainId = ElephantStains[_rng.Next(0, ElephantStains.Length)];
                SetElephant(EquipSlot.Body, ref armor[1], stainId);
                SetElephant(EquipSlot.Head, ref armor[0], stainId);
                break;
            case CodeService.CodeFlag.Dolphins:
                SetDolphin(EquipSlot.Body, ref armor[1]);
                SetDolphin(EquipSlot.Head, ref armor[0]); 
                break;
            case CodeService.CodeFlag.World when actor.Index != 0:
                _worldSets.Apply(actor, _rng, armor);
                break;
        }

        switch (_codes.Masked(CodeService.DyeCodes))
        {
            case CodeService.CodeFlag.Clown:
                foreach (ref var piece in armor)
                    SetRandomDye(ref piece);
                break;
        }
    }

    public void ApplyFunToWeapon(Actor actor, ref CharacterWeapon weapon, EquipSlot slot)
    {
        if (!ValidFunTarget(actor))
            return;

        if (_codes.Enabled(CodeService.CodeFlag.World) && actor.Index != 0)
            _worldSets.Apply(actor, _rng, ref weapon, slot);
    }

    private static bool ValidFunTarget(Actor actor)
        => actor.IsCharacter
         && actor.AsObject->ObjectKind is (byte)ObjectKind.Player
         && !actor.IsTransformed
         && actor.AsCharacter->CharacterData.ModelCharaId == 0;

    private static void KeepOldArmor(Actor actor, EquipSlot slot, ref CharacterArmor armor)
        => armor = actor.Model.Valid ? actor.Model.GetArmor(slot) : armor;

    private void SetRandomDye(ref CharacterArmor armor)
    {
        var stainIdx = _rng.Next(0, _stains.Length - 1);
        armor.Stain = _stains[stainIdx];
    }

    private void SetRandomItem(EquipSlot slot, ref CharacterArmor armor)
    {
        var list = _items.ItemData.ByType[slot.ToEquipType()];
        var rng  = _rng.Next(0, list.Count - 1);
        var item = list[rng];
        armor.Set     = item.PrimaryId;
        armor.Variant = item.Variant;
    }

    private static ReadOnlySpan<byte> ElephantStains
        =>
        [
            87, 87, 87, 87, 87, // Cherry Pink
            83, 83, 83,         // Colibri Pink 
            80,                 // Iris Purple
            85,                 // Regal Purple
            103,                // Pastel Pink
            82, 82, 82,         // Lotus Pink
            7,                  // Rose Pink
        ];

    private static IReadOnlyList<CharacterArmor> DolphinBodies
        =>
        [
            new CharacterArmor(6089, 1, 4), // Toad
            new CharacterArmor(6089, 1, 4), // Toad
            new CharacterArmor(6089, 1, 4), // Toad
            new CharacterArmor(6023, 1, 4), // Swine
            new CharacterArmor(6023, 1, 4), // Swine
            new CharacterArmor(6023, 1, 4), // Swine
            new CharacterArmor(6133, 1, 4), // Gaja
            new CharacterArmor(6182, 1, 3), // Imp
            new CharacterArmor(6182, 1, 3), // Imp
            new CharacterArmor(6182, 1, 4), // Imp
            new CharacterArmor(6182, 1, 4), // Imp
        ];

    private void SetDolphin(EquipSlot slot, ref CharacterArmor armor)
    {
        armor = slot switch
        {
            EquipSlot.Body => DolphinBodies[_rng.Next(0, DolphinBodies.Count - 1)],
            EquipSlot.Head => new CharacterArmor(5040, 1, 0),
            _              => armor,
        };
    }

    private void SetElephant(EquipSlot slot, ref CharacterArmor armor, StainId stainId)
    {
        armor = slot switch
        {
            EquipSlot.Body => new CharacterArmor(6133, 1, stainId),
            EquipSlot.Head => new CharacterArmor(6133, 1, stainId),
            _              => armor,
        };
    }

    private static void SetCrown(Span<CharacterArmor> armor)
    {
        var clown = new CharacterArmor(6117, 1, 0);
        armor[0] = clown;
        armor[1] = clown;
        armor[2] = clown;
        armor[3] = clown;
        armor[4] = clown;
    }

    private void SetRace(ref CustomizeArray customize)
    {
        var race = _codes.GetRace();
        if (race == Race.Unknown)
            return;

        var targetClan = (SubRace)((int)race * 2 - (int)customize.Clan % 2);
        // TODO Female Hrothgar
        if (race is Race.Hrothgar && customize.Gender is Gender.Female)
            targetClan = targetClan is SubRace.Lost ? SubRace.Seawolf : SubRace.Hellsguard;
        _customizations.ChangeClan(ref customize, targetClan);
    }

    private void SetGender(ref CustomizeArray customize)
    {
        if (!_codes.Enabled(CodeService.CodeFlag.SixtyThree) || customize.Race is Race.Hrothgar) // TODO Female Hrothgar
            return;

        _customizations.ChangeGender(ref customize, customize.Gender is Gender.Male ? Gender.Female : Gender.Male);
    }

    private void RandomizeCustomize(ref CustomizeArray customize)
    {
        if (!_codes.Enabled(CodeService.CodeFlag.Individual))
            return;

        var set = _customizations.Manager.GetSet(customize.Clan, customize.Gender);
        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            if (index is CustomizeIndex.Face || !set.IsAvailable(index))
                continue;

            var valueIdx = _rng.Next(0, set.Count(index) - 1);
            customize[index] = set.Data(index, valueIdx).Value;
        }
    }

    private void SetSize(Actor actor, ref CustomizeArray customize)
    {
        var size = _codes.Masked(CodeService.SizeCodes) switch
        {
            CodeService.CodeFlag.Dwarf when actor.Index == 0 => (byte)0,
            CodeService.CodeFlag.Dwarf                       => (byte)100,
            CodeService.CodeFlag.Giant when actor.Index == 0 => (byte)100,
            CodeService.CodeFlag.Giant                       => (byte)0,
            _                                                => byte.MaxValue,
        };
        if (size == byte.MaxValue)
            return;

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
            tmp.SetDesignData(_customizations, _stateManager.FromActor(actor, true, true));
            var data = _designConverter.ShareBase64(tmp);
            ImGui.SetClipboardText(data);
            Glamourer.Messager.NotificationMessage($"Copied current actual design of {actor.Utf8Name} to clipboard.", NotificationType.Info,
                false);
        }
        catch
        {
            // ignored
        }
    }
}
