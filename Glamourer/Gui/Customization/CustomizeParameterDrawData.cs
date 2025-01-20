using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.State;

namespace Glamourer.Gui.Customization;

public struct CustomizeParameterDrawData(CustomizeParameterFlag flag, in DesignData data)
{
    private         IDesignEditor          _editor = null!;
    private         object                 _object = null!;
    public readonly CustomizeParameterFlag Flag = flag;
    public          bool                   Locked;
    public          bool                   DisplayApplication;
    public          bool                   AllowRevert;

    public readonly void ChangeParameter(CustomizeParameterValue value)
        => _editor.ChangeCustomizeParameter(_object, Flag, value, ApplySettings.Manual);

    public readonly void ChangeApplyParameter(bool value)
    {
        var manager = (DesignManager)_editor;
        var design  = (Design)_object;
        manager.ChangeApplyParameter(design, Flag, value);
    }

    public CustomizeParameterValue         CurrentValue = data.Parameters[flag];
    public CustomizeParameterValue         GameValue;
    public bool                            CurrentApply;

    public static CustomizeParameterDrawData FromDesign(DesignManager manager, Design design, CustomizeParameterFlag flag)
        => new(flag, design.DesignData)
        {
            _editor = manager,
            _object = design,
            Locked             = design.WriteProtected(),
            DisplayApplication = true,
            CurrentApply       = design.DoApplyParameter(flag),
        };

    public static CustomizeParameterDrawData FromState(StateManager manager, ActorState state, CustomizeParameterFlag flag)
        => new(flag, state.ModelData)
        {
            _editor            = manager,
            _object            = state,
            Locked             = state.IsLocked,
            DisplayApplication = false,
            GameValue          = state.BaseData.Parameters[flag],
            AllowRevert        = true,
        };
}
