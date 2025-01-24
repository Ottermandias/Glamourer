using Glamourer.Api.Enums;
using Glamourer.GameData;
using ImGuiNET;
using Penumbra.GameData.Enums;

namespace Glamourer.Designs;

public record struct ApplicationCollection(
    EquipFlag Equip,
    BonusItemFlag BonusItem,
    CustomizeFlag CustomizeRaw,
    CrestFlag Crest,
    CustomizeParameterFlag Parameters,
    MetaFlag Meta)
{
    public static readonly ApplicationCollection All = new(EquipFlagExtensions.All, BonusExtensions.All,
        CustomizeFlagExtensions.AllRelevant, CrestExtensions.AllRelevant, CustomizeParameterExtensions.All, MetaExtensions.All);

    public static readonly ApplicationCollection None = new(0, 0, CustomizeFlag.BodyType, 0, 0, 0);

    public static readonly ApplicationCollection Equipment = new(EquipFlagExtensions.All, BonusExtensions.All,
        CustomizeFlag.BodyType, CrestExtensions.AllRelevant, 0, MetaFlag.HatState | MetaFlag.WeaponState | MetaFlag.VisorState);

    public static readonly ApplicationCollection Customizations = new(0, 0, CustomizeFlagExtensions.AllRelevant, 0,
        CustomizeParameterExtensions.All, MetaFlag.Wetness);

    public static readonly ApplicationCollection Default = new(EquipFlagExtensions.All, BonusExtensions.All,
        CustomizeFlagExtensions.AllRelevant, CrestExtensions.AllRelevant, 0, MetaFlag.HatState | MetaFlag.VisorState | MetaFlag.WeaponState);

    public static ApplicationCollection FromKeys()
        => (ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift) switch
        {
            (false, false) => All,
            (true, true)   => All,
            (true, false)  => Equipment,
            (false, true)  => Customizations,
        };

    public CustomizeFlag Customize
    {
        get => CustomizeRaw;
        set => CustomizeRaw = value | CustomizeFlag.BodyType;
    }

    public void RemoveEquip()
    {
        Equip     =  0;
        BonusItem =  0;
        Crest     =  0;
        Meta      &= ~(MetaFlag.HatState | MetaFlag.VisorState | MetaFlag.WeaponState);
    }

    public void RemoveCustomize()
    {
        Customize  =  0;
        Parameters =  0;
        Meta       &= MetaFlag.Wetness;
    }

    public ApplicationCollection Restrict(ApplicationCollection old)
        => new(old.Equip & Equip, old.BonusItem & BonusItem, (old.Customize & Customize) | CustomizeFlag.BodyType, old.Crest & Crest,
            old.Parameters & Parameters, old.Meta & Meta);

    public ApplicationCollection CloneSecure()
        => new(Equip & EquipFlagExtensions.All, BonusItem & BonusExtensions.All,
            (Customize & CustomizeFlagExtensions.AllRelevant) | CustomizeFlag.BodyType, Crest & CrestExtensions.AllRelevant,
            Parameters & CustomizeParameterExtensions.All, Meta & MetaExtensions.All);
}
