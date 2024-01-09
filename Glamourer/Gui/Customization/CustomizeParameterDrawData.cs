using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.State;

namespace Glamourer.Gui.Customization;

public ref struct CustomizeParameterDrawData(CustomizeParameterFlag flag, in DesignData data)
{
    public readonly CustomizeParameterFlag Flag = flag;
    public          bool                   Locked;
    public          bool                   DisplayApplication;
    public          bool                   AllowRevert;

    public Action<CustomizeParameterValue> ValueSetter  = null!;
    public Action<bool>                    ApplySetter  = null!;
    public CustomizeParameterValue         CurrentValue = data.Parameters[flag];
    public CustomizeParameterValue         GameValue;
    public bool                            CurrentApply;

    public static CustomizeParameterDrawData FromDesign(DesignManager manager, Design design, CustomizeParameterFlag flag)
        => new(flag, design.DesignData)
        {
            Locked             = design.WriteProtected(),
            DisplayApplication = true,
            CurrentApply       = design.DoApplyParameter(flag),
            ValueSetter        = v => manager.ChangeCustomizeParameter(design, flag, v),
            ApplySetter        = v => manager.ChangeApplyParameter(design, flag, v),
        };

    public static CustomizeParameterDrawData FromState(StateManager manager, ActorState state, CustomizeParameterFlag flag)
        => new(flag, state.ModelData)
        {
            Locked             = state.IsLocked,
            DisplayApplication = false,
            ValueSetter        = v => manager.ChangeCustomizeParameter(state, flag, v, StateChanged.Source.Manual),
            GameValue          = state.BaseData.Parameters[flag],
            AllowRevert        = true,
        };
}
