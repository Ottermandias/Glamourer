using Glamourer.GameData;
using Glamourer.State;
using ImGuiNET;
using Penumbra.GameData.Enums;

namespace Glamourer.Designs;

public readonly struct ApplicationRules(
    EquipFlag equip,
    CustomizeFlag customize,
    CrestFlag crest,
    CustomizeParameterFlag parameters,
    MetaFlag meta)
{
    public static readonly ApplicationRules All = new(EquipFlagExtensions.All, CustomizeFlagExtensions.AllRelevant,
        CrestExtensions.AllRelevant, CustomizeParameterExtensions.All, MetaExtensions.All);

    public static ApplicationRules FromModifiers(ActorState state)
        => FromModifiers(state, ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift);

    public static ApplicationRules NpcFromModifiers()
        => NpcFromModifiers(ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift);

    public static ApplicationRules AllButParameters(ActorState state)
        => new(All.Equip, All.Customize, All.Crest, ComputeParameters(state.ModelData, state.BaseData, All.Parameters), All.Meta);

    public static ApplicationRules NpcFromModifiers(bool ctrl, bool shift)
        => new(ctrl || !shift ? EquipFlagExtensions.All : 0,
            !ctrl || shift ? CustomizeFlagExtensions.AllRelevant : 0,
            0,
            0,
            ctrl || !shift ? MetaFlag.VisorState : 0);

    public static ApplicationRules FromModifiers(ActorState state, bool ctrl, bool shift)
    {
        var equip      = ctrl || !shift ? EquipFlagExtensions.All : 0;
        var customize  = !ctrl || shift ? CustomizeFlagExtensions.AllRelevant : 0;
        var crest      = equip == 0 ? 0 : CrestExtensions.AllRelevant;
        var parameters = customize == 0 ? 0 : CustomizeParameterExtensions.All;
        var meta       = state.ModelData.IsWet() ? MetaFlag.Wetness : 0;
        if (equip != 0)
            meta |= MetaFlag.HatState | MetaFlag.WeaponState | MetaFlag.VisorState;

        return new ApplicationRules(equip, customize, crest, ComputeParameters(state.ModelData, state.BaseData, parameters), meta);
    }

    public void Apply(DesignBase design)
    {
        design.ApplyEquip      = Equip;
        design.ApplyCustomize  = Customize;
        design.ApplyCrest      = Crest;
        design.ApplyParameters = Parameters;
        design.ApplyMeta       = Meta;
    }

    public EquipFlag Equip
        => equip & EquipFlagExtensions.All;

    public CustomizeFlag Customize
        => customize & CustomizeFlagExtensions.AllRelevant;

    public CrestFlag Crest
        => crest & CrestExtensions.AllRelevant;

    public CustomizeParameterFlag Parameters
        => parameters & CustomizeParameterExtensions.All;

    public MetaFlag Meta
        => meta & MetaExtensions.All;

    public static CustomizeParameterFlag ComputeParameters(in DesignData model, in DesignData game,
        CustomizeParameterFlag baseFlags = CustomizeParameterExtensions.All)
    {
        foreach (var flag in baseFlags.Iterate())
        {
            var modelValue = model.Parameters[flag];
            var gameValue  = game.Parameters[flag];
            if ((modelValue.InternalQuadruple - gameValue.InternalQuadruple).LengthSquared() > 1e-9f)
                baseFlags &= ~flag;
        }

        return baseFlags;
    }
}
