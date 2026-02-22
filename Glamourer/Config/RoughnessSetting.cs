using Luna.Generators;

namespace Glamourer.Config;

[NamedEnum(Utf16: false)]
public enum RoughnessSetting
{
    [Name("As-Is")]
    AsIs,

    [Name("Always Roughness")]
    AlwaysRoughness,

    [Name("Always Gloss Strength")]
    AlwaysGloss,
}

public static partial class RoughnessSettingExtensions
{
    public static bool Get(this RoughnessSetting setting, bool roughness)
        => setting switch
        {
            RoughnessSetting.AlwaysRoughness => true,
            RoughnessSetting.AlwaysGloss     => false,
            _                                => roughness,
        };
}
