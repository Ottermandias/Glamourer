using Glamourer.Designs;
using Glamourer.State;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public struct BonusDrawData(BonusItemFlag slot, in DesignData designData)
{
    private         IDesignEditor _editor = null!;
    private         object        _object = null!;
    public readonly BonusItemFlag Slot = slot;
    public          bool          Locked;
    public          bool          DisplayApplication;
    public          bool          AllowRevert;

    public readonly bool IsDesign
        => _object is Design;

    public readonly bool IsState
        => _object is ActorState;

    public readonly void SetItem(EquipItem item)
        => _editor.ChangeBonusItem(_object, Slot, item, ApplySettings.Manual);

    public readonly void SetApplyItem(bool value)
    {
        var manager = (DesignManager)_editor;
        var design  = (Design)_object;
        manager.ChangeApplyBonusItem(design, Slot, value);
    }

    public EquipItem CurrentItem = designData.BonusItem(slot);
    public EquipItem GameItem    = default;
    public bool      CurrentApply;

    public static BonusDrawData FromDesign(DesignManager manager, Design design, BonusItemFlag slot)
        => new(slot, design.DesignData)
        {
            _editor            = manager,
            _object            = design,
            CurrentApply       = design.DoApplyBonusItem(slot),
            Locked             = design.WriteProtected(),
            DisplayApplication = true,
        };

    public static BonusDrawData FromState(StateManager manager, ActorState state, BonusItemFlag slot)
        => new(slot, state.ModelData)
        {
            _editor            = manager,
            _object            = state,
            Locked             = state.IsLocked,
            DisplayApplication = false,
            GameItem           = state.BaseData.BonusItem(slot),
            AllowRevert        = true,
        };
}
