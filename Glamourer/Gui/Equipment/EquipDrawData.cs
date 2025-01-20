using Glamourer.Designs;
using Glamourer.State;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public struct EquipDrawData(EquipSlot slot, in DesignData designData)
{
    private         IDesignEditor _editor = null!;
    private         object        _object = null!;
    public readonly EquipSlot     Slot = slot;
    public          bool          Locked;
    public          bool          DisplayApplication;
    public          bool          AllowRevert;

    public readonly bool IsDesign
        => _object is Design;

    public readonly bool IsState
        => _object is ActorState;

    public readonly void SetItem(EquipItem item)
        => _editor.ChangeItem(_object, Slot, item, ApplySettings.Manual);

    public readonly void SetStains(StainIds stains)
        => _editor.ChangeStains(_object, Slot, stains, ApplySettings.Manual);

    public readonly void SetApplyItem(bool value)
    {
        var manager = (DesignManager)_editor;
        var design  = (Design)_object;
        manager.ChangeApplyItem(design, Slot, value);
    }

    public readonly void SetApplyStain(bool value)
    {
        var manager = (DesignManager)_editor;
        var design  = (Design)_object;
        manager.ChangeApplyStains(design, Slot, value);
    }

    public EquipItem CurrentItem   = designData.Item(slot);
    public StainIds  CurrentStains = designData.Stain(slot);
    public EquipItem GameItem      = default;
    public StainIds  GameStains    = default;
    public bool      CurrentApply;
    public bool      CurrentApplyStain;

    public readonly Gender CurrentGender = designData.Customize.Gender;
    public readonly Race   CurrentRace   = designData.Customize.Race;

    public static EquipDrawData FromDesign(DesignManager manager, Design design, EquipSlot slot)
        => new(slot, design.DesignData)
        {
            _editor            = manager,
            _object            = design,
            CurrentApply       = design.DoApplyEquip(slot),
            CurrentApplyStain  = design.DoApplyStain(slot),
            Locked             = design.WriteProtected(),
            DisplayApplication = true,
        };

    public static EquipDrawData FromState(StateManager manager, ActorState state, EquipSlot slot)
        => new(slot, state.ModelData)
        {
            _editor            = manager,
            _object            = state,
            Locked             = state.IsLocked,
            DisplayApplication = false,
            GameItem           = state.BaseData.Item(slot),
            GameStains         = state.BaseData.Stain(slot),
            AllowRevert        = true,
        };
}