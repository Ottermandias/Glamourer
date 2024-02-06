using Glamourer.Designs;
using Glamourer.State;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui;

public struct ToggleDrawData
{
    private IDesignEditor _editor = null!;
    private object        _data   = null!;
    private StateIndex    _index;

    public bool Locked;
    public bool DisplayApplication;

    public bool CurrentValue;
    public bool CurrentApply;

    public string Label   = string.Empty;
    public string Tooltip = string.Empty;


    public ToggleDrawData()
    { }

    public readonly void SetValue(bool value)
    {
        switch (_index.GetFlag())
        {
            case MetaFlag flag:
                _editor.ChangeMetaState(_data, flag.ToIndex(), value, ApplySettings.Manual);
                break;
            case CrestFlag flag:
                _editor.ChangeCrest(_data, flag, value, ApplySettings.Manual);
                break;
        }
    }

    public readonly void SetApply(bool value)
    {
        var manager = (DesignManager)_editor;
        var design  = (Design)_data;
        switch (_index.GetFlag())
        {
            case MetaFlag flag:
                manager.ChangeApplyMeta(design, flag.ToIndex(), value);
                break;
            case CrestFlag flag:
                manager.ChangeApplyCrest(design, flag, value);
                break;
        }
    }

    public static ToggleDrawData FromDesign(MetaIndex index, DesignManager manager, Design design)
        => new()
        {
            _index             = index,
            _editor            = manager,
            _data              = design,
            Label              = index.ToName(),
            Tooltip            = string.Empty,
            Locked             = design.WriteProtected(),
            DisplayApplication = true,
            CurrentValue       = design.DesignData.GetMeta(index),
            CurrentApply       = design.DoApplyMeta(index),
        };

    public static ToggleDrawData FromState(MetaIndex index, StateManager manager, ActorState state)
        => new()
        {
            _index       = index,
            _editor      = manager,
            _data        = state,
            Label        = index.ToName(),
            Tooltip      = index.ToTooltip(),
            Locked       = state.IsLocked,
            CurrentValue = state.ModelData.GetMeta(index),
        };

    public static ToggleDrawData CrestFromDesign(CrestFlag slot, DesignManager manager, Design design)
        => new()
        {
            _index             = slot,
            _editor            = manager,
            _data              = design,
            Label              = $"{slot.ToLabel()} Crest",
            Tooltip            = string.Empty,
            Locked             = design.WriteProtected(),
            DisplayApplication = true,
            CurrentValue       = design.DesignData.Crest(slot),
            CurrentApply       = design.DoApplyCrest(slot),
        };

    public static ToggleDrawData CrestFromState(CrestFlag slot, StateManager manager, ActorState state)
        => new()
        {
            _index       = slot,
            _editor      = manager,
            _data        = state,
            Label        = $"{slot.ToLabel()} Crest",
            Tooltip      = "Hide or show your free company crest on this piece of gear.",
            Locked       = state.IsLocked,
            CurrentValue = state.ModelData.Crest(slot),
        };

    public static ToggleDrawData FromValue(MetaIndex index, bool value)
        => new()
        {
            _index       = index,
            Label        = index.ToName(),
            Tooltip      = index.ToTooltip(),
            Locked       = true,
            CurrentValue = value,
        };
}
