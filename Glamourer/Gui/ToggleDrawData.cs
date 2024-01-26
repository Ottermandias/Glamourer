using Glamourer.Designs;
using Glamourer.State;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui;

public ref struct ToggleDrawData
{
    public bool Locked;
    public bool DisplayApplication;

    public bool CurrentValue;
    public bool CurrentApply;

    public Action<bool> SetValue = null!;
    public Action<bool> SetApply = null!;

    public string Label   = string.Empty;
    public string Tooltip = string.Empty;

    public ToggleDrawData()
    { }

    public static ToggleDrawData FromDesign(MetaIndex index, DesignManager manager, Design design)
        => new()
        {
            Label              = index.ToName(),
            Tooltip            = string.Empty,
            Locked             = design.WriteProtected(),
            DisplayApplication = true,
            CurrentValue       = design.DesignData.GetMeta(index),
            CurrentApply       = design.DoApplyMeta(index),
            SetValue           = b => manager.ChangeMetaState(design, index, b),
            SetApply           = b => manager.ChangeApplyMeta(design, index, b),
        };

    public static ToggleDrawData CrestFromDesign(CrestFlag slot, DesignManager manager, Design design)
        => new()
        {
            Label              = $"{slot.ToLabel()} Crest",
            Tooltip            = string.Empty,
            Locked             = design.WriteProtected(),
            DisplayApplication = true,
            CurrentValue       = design.DesignData.Crest(slot),
            CurrentApply       = design.DoApplyCrest(slot),
            SetValue           = v => manager.ChangeCrest(design, slot, v),
            SetApply           = v => manager.ChangeApplyCrest(design, slot, v),
        };

    public static ToggleDrawData CrestFromState(CrestFlag slot, StateManager manager, ActorState state)
        => new()
        {
            Label        = $"{slot.ToLabel()} Crest",
            Tooltip      = "Hide or show your free company crest on this piece of gear.",
            Locked       = state.IsLocked,
            CurrentValue = state.ModelData.Crest(slot),
            SetValue     = v => manager.ChangeCrest(state, slot, v, ApplySettings.Manual),
        };

    public static ToggleDrawData FromState(MetaIndex index, StateManager manager, ActorState state)
    {
        return new ToggleDrawData
        {
            Label        = index.ToName(),
            Tooltip      = index.ToTooltip(),
            Locked       = state.IsLocked,
            CurrentValue = state.ModelData.GetMeta(index),
            SetValue     = b => manager.ChangeMetaState(state, index, b, ApplySettings.Manual),
        };
    }

    public static ToggleDrawData FromValue(MetaIndex index, bool value)
        => new()
        {
            Label        = index.ToName(),
            Tooltip      = index.ToTooltip(),
            Locked       = true,
            CurrentValue = value,
        };
}
