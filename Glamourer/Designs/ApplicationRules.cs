using Glamourer.Api.Enums;
using Glamourer.GameData;
using Glamourer.State;
using ImGuiNET;
using Penumbra.GameData.Enums;

namespace Glamourer.Designs;

public readonly struct ApplicationRules(ApplicationCollection application, bool materials)
{
    public static readonly ApplicationRules All = new(ApplicationCollection.All, true);

    public static ApplicationRules FromModifiers(ActorState state)
        => FromModifiers(state, ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift);

    public static ApplicationRules NpcFromModifiers()
        => NpcFromModifiers(ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift);

    public static ApplicationRules AllButParameters(ActorState state)
        => new(ApplicationCollection.All with { Parameters = ComputeParameters(state.ModelData, state.BaseData, All.Parameters) }, true);

    public static ApplicationRules AllWithConfig(Configuration config)
        => new(ApplicationCollection.All with { Parameters = config.UseAdvancedParameters ? All.Parameters : 0 }, config.UseAdvancedDyes);

    public static ApplicationRules NpcFromModifiers(bool ctrl, bool shift)
    {
        var equip     = ctrl || !shift ? EquipFlagExtensions.All : 0;
        var customize = !ctrl || shift ? CustomizeFlagExtensions.AllRelevant : 0;
        var visor     = equip != 0 ? MetaFlag.VisorState : 0;
        return new ApplicationRules(new ApplicationCollection(equip, 0, customize, 0, 0, visor), false);
    }

    public static ApplicationRules FromModifiers(ActorState state, bool ctrl, bool shift)
    {
        var equip      = ctrl || !shift ? EquipFlagExtensions.All : 0;
        var customize  = !ctrl || shift ? CustomizeFlagExtensions.AllRelevant : 0;
        var bonus      = equip == 0 ? 0 : BonusExtensions.All;
        var crest      = equip == 0 ? 0 : CrestExtensions.AllRelevant;
        var parameters = customize == 0 ? 0 : CustomizeParameterExtensions.All;
        var meta       = state.ModelData.IsWet() ? MetaFlag.Wetness : 0;
        if (equip != 0)
            meta |= MetaFlag.HatState | MetaFlag.WeaponState | MetaFlag.VisorState;

        var collection = new ApplicationCollection(equip, bonus, customize, crest,
            ComputeParameters(state.ModelData, state.BaseData, parameters), meta);
        return new ApplicationRules(collection, equip != 0);
    }

    public void Apply(DesignBase design)
        => design.Application = application;

    public EquipFlag Equip
        => application.Equip & EquipFlagExtensions.All;

    public CustomizeParameterFlag Parameters
        => application.Parameters & CustomizeParameterExtensions.All;

    public bool Materials
        => materials;

    private static CustomizeParameterFlag ComputeParameters(in DesignData model, in DesignData game,
        CustomizeParameterFlag baseFlags = CustomizeParameterExtensions.All)
    {
        foreach (var flag in baseFlags.Iterate())
        {
            var modelValue = model.Parameters[flag];
            var gameValue  = game.Parameters[flag];
            if (modelValue.NearEqual(gameValue))
                baseFlags &= ~flag;
        }

        return baseFlags;
    }
}
