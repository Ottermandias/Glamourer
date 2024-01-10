using FFXIVClientStructs.FFXIV.Shader;

namespace Glamourer.GameData;

public struct CustomizeParameterData
{
    public Vector4 DecalColor;
    public Vector4 LipDiffuse;
    public Vector3 SkinDiffuse;
    public Vector3 SkinSpecular;
    public Vector3 HairDiffuse;
    public Vector3 HairSpecular;
    public Vector3 HairHighlight;
    public Vector3 LeftEye;
    public Vector3 RightEye;
    public Vector3 FeatureColor;
    public float   FacePaintUvMultiplier;
    public float   FacePaintUvOffset;
    public float   MuscleTone;

    public CustomizeParameterValue this[CustomizeParameterFlag flag]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        readonly get
        {
            return flag switch
            {
                CustomizeParameterFlag.SkinDiffuse           => new CustomizeParameterValue(SkinDiffuse),
                CustomizeParameterFlag.MuscleTone            => new CustomizeParameterValue(MuscleTone),
                CustomizeParameterFlag.SkinSpecular          => new CustomizeParameterValue(SkinSpecular),
                CustomizeParameterFlag.LipDiffuse            => new CustomizeParameterValue(LipDiffuse),
                CustomizeParameterFlag.HairDiffuse           => new CustomizeParameterValue(HairDiffuse),
                CustomizeParameterFlag.HairSpecular          => new CustomizeParameterValue(HairSpecular),
                CustomizeParameterFlag.HairHighlight         => new CustomizeParameterValue(HairHighlight),
                CustomizeParameterFlag.LeftEye               => new CustomizeParameterValue(LeftEye),
                CustomizeParameterFlag.RightEye              => new CustomizeParameterValue(RightEye),
                CustomizeParameterFlag.FeatureColor          => new CustomizeParameterValue(FeatureColor),
                CustomizeParameterFlag.DecalColor            => new CustomizeParameterValue(DecalColor),
                CustomizeParameterFlag.FacePaintUvMultiplier => new CustomizeParameterValue(FacePaintUvMultiplier),
                CustomizeParameterFlag.FacePaintUvOffset     => new CustomizeParameterValue(FacePaintUvOffset),
                _                                            => CustomizeParameterValue.Zero,
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        set => Set(flag, value);
    }

    public bool Set(CustomizeParameterFlag flag, CustomizeParameterValue value)
    {
        return flag switch
        {
            CustomizeParameterFlag.SkinDiffuse           => SetIfDifferent(ref SkinDiffuse,           value.InternalTriple),
            CustomizeParameterFlag.MuscleTone            => SetIfDifferent(ref MuscleTone,            value.Single),
            CustomizeParameterFlag.SkinSpecular          => SetIfDifferent(ref SkinSpecular,          value.InternalTriple),
            CustomizeParameterFlag.LipDiffuse            => SetIfDifferent(ref LipDiffuse,            value.InternalQuadruple),
            CustomizeParameterFlag.HairDiffuse           => SetIfDifferent(ref HairDiffuse,           value.InternalTriple),
            CustomizeParameterFlag.HairSpecular          => SetIfDifferent(ref HairSpecular,          value.InternalTriple),
            CustomizeParameterFlag.HairHighlight         => SetIfDifferent(ref HairHighlight,         value.InternalTriple),
            CustomizeParameterFlag.LeftEye               => SetIfDifferent(ref LeftEye,               value.InternalTriple),
            CustomizeParameterFlag.RightEye              => SetIfDifferent(ref RightEye,              value.InternalTriple),
            CustomizeParameterFlag.FeatureColor          => SetIfDifferent(ref FeatureColor,          value.InternalTriple),
            CustomizeParameterFlag.DecalColor            => SetIfDifferent(ref DecalColor,            value.InternalQuadruple),
            CustomizeParameterFlag.FacePaintUvMultiplier => SetIfDifferent(ref FacePaintUvMultiplier, value.Single),
            CustomizeParameterFlag.FacePaintUvOffset     => SetIfDifferent(ref FacePaintUvOffset,     value.Single),
            _                                            => false,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public readonly void Apply(ref CustomizeParameter parameters, CustomizeParameterFlag flags = CustomizeParameterExtensions.All)
    {
        parameters.SkinColor = (flags & (CustomizeParameterFlag.SkinDiffuse | CustomizeParameterFlag.MuscleTone)) switch
        {
            0                                  => parameters.SkinColor,
            CustomizeParameterFlag.SkinDiffuse => new CustomizeParameterValue(SkinDiffuse, parameters.SkinColor.W).XivQuadruple,
            CustomizeParameterFlag.MuscleTone  => parameters.SkinColor with { W = MuscleTone },
            _                                  => new CustomizeParameterValue(SkinDiffuse, MuscleTone).XivQuadruple,
        };

        parameters.LeftColor = (flags & (CustomizeParameterFlag.LeftEye | CustomizeParameterFlag.FacePaintUvMultiplier)) switch
        {
            0                                            => parameters.LeftColor,
            CustomizeParameterFlag.LeftEye               => new CustomizeParameterValue(LeftEye, parameters.LeftColor.W).XivQuadruple,
            CustomizeParameterFlag.FacePaintUvMultiplier => parameters.LeftColor with { W = FacePaintUvMultiplier },
            _                                            => new CustomizeParameterValue(LeftEye, FacePaintUvMultiplier).XivQuadruple,
        };

        parameters.RightColor = (flags & (CustomizeParameterFlag.RightEye | CustomizeParameterFlag.FacePaintUvOffset)) switch
        {
            0                                        => parameters.RightColor,
            CustomizeParameterFlag.RightEye          => new CustomizeParameterValue(RightEye, parameters.RightColor.W).XivQuadruple,
            CustomizeParameterFlag.FacePaintUvOffset => parameters.RightColor with { W = FacePaintUvOffset },
            _                                        => new CustomizeParameterValue(RightEye, FacePaintUvOffset).XivQuadruple,
        };

        if (flags.HasFlag(CustomizeParameterFlag.SkinSpecular))
            parameters.SkinFresnelValue0 = new CustomizeParameterValue(SkinSpecular).XivQuadruple;
        if (flags.HasFlag(CustomizeParameterFlag.HairDiffuse))
            parameters.MainColor = new CustomizeParameterValue(HairDiffuse).XivTriple;
        if (flags.HasFlag(CustomizeParameterFlag.HairSpecular))
            parameters.HairFresnelValue0 = new CustomizeParameterValue(HairSpecular).XivTriple;
        if (flags.HasFlag(CustomizeParameterFlag.HairHighlight))
            parameters.MeshColor = new CustomizeParameterValue(HairHighlight).XivTriple;
        if (flags.HasFlag(CustomizeParameterFlag.LipDiffuse))
            parameters.LipColor = new CustomizeParameterValue(LipDiffuse).XivQuadruple;
        if (flags.HasFlag(CustomizeParameterFlag.FeatureColor))
            parameters.OptionColor = new CustomizeParameterValue(FeatureColor).XivTriple;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public readonly void Apply(ref DecalParameters parameters, CustomizeParameterFlag flags = CustomizeParameterExtensions.All)
    {
        if (flags.HasFlag(CustomizeParameterFlag.DecalColor))
            parameters.Color = new CustomizeParameterValue(DecalColor).XivQuadruple;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public readonly void ApplySingle(ref CustomizeParameter parameters, CustomizeParameterFlag flag)
    {
        switch (flag)
        {
            case CustomizeParameterFlag.SkinDiffuse:
                parameters.SkinColor = new CustomizeParameterValue(SkinDiffuse, parameters.SkinColor.W).XivQuadruple;
                break;
            case CustomizeParameterFlag.MuscleTone:
                parameters.SkinColor.W = MuscleTone;
                break;
            case CustomizeParameterFlag.SkinSpecular:
                parameters.SkinFresnelValue0 = new CustomizeParameterValue(SkinSpecular).XivQuadruple;
                break;
            case CustomizeParameterFlag.LipDiffuse:
                parameters.LipColor = new CustomizeParameterValue(LipDiffuse).XivQuadruple;
                break;
            case CustomizeParameterFlag.HairDiffuse:
                parameters.MainColor = new CustomizeParameterValue(HairDiffuse).XivTriple;
                break;
            case CustomizeParameterFlag.HairSpecular:
                parameters.HairFresnelValue0 = new CustomizeParameterValue(HairSpecular).XivTriple;
                break;
            case CustomizeParameterFlag.HairHighlight:
                parameters.MeshColor = new CustomizeParameterValue(HairHighlight).XivTriple;
                break;
            case CustomizeParameterFlag.LeftEye:
                parameters.LeftColor = new CustomizeParameterValue(LeftEye, parameters.LeftColor.W).XivQuadruple;
                break;
            case CustomizeParameterFlag.RightEye:
                parameters.RightColor = new CustomizeParameterValue(RightEye, parameters.RightColor.W).XivQuadruple;
                break;
            case CustomizeParameterFlag.FeatureColor:
                parameters.OptionColor = new CustomizeParameterValue(FeatureColor).XivTriple;
                break;
            case CustomizeParameterFlag.FacePaintUvMultiplier:
                parameters.LeftColor.W = FacePaintUvMultiplier;
                break;
            case CustomizeParameterFlag.FacePaintUvOffset:
                parameters.RightColor.W = FacePaintUvOffset;
                break;
        }
    }

    public static CustomizeParameterData FromParameters(in CustomizeParameter parameter, in DecalParameters decal)
        => new()
        {
            FacePaintUvOffset     = parameter.RightColor.W,
            FacePaintUvMultiplier = parameter.LeftColor.W,
            MuscleTone            = parameter.SkinColor.W,
            SkinDiffuse           = new CustomizeParameterValue(parameter.SkinColor).InternalTriple,
            SkinSpecular          = new CustomizeParameterValue(parameter.SkinFresnelValue0).InternalTriple,
            LipDiffuse            = new CustomizeParameterValue(parameter.LipColor).InternalQuadruple,
            HairDiffuse           = new CustomizeParameterValue(parameter.MainColor).InternalTriple,
            HairSpecular          = new CustomizeParameterValue(parameter.HairFresnelValue0).InternalTriple,
            HairHighlight         = new CustomizeParameterValue(parameter.MeshColor).InternalTriple,
            LeftEye               = new CustomizeParameterValue(parameter.LeftColor).InternalTriple,
            RightEye              = new CustomizeParameterValue(parameter.RightColor).InternalTriple,
            FeatureColor          = new CustomizeParameterValue(parameter.OptionColor).InternalTriple,
            DecalColor            = FromParameter(decal),
        };

    public static CustomizeParameterValue FromParameter(in CustomizeParameter parameter, CustomizeParameterFlag flag)
        => flag switch
        {
            CustomizeParameterFlag.SkinDiffuse           => new CustomizeParameterValue(parameter.SkinColor),
            CustomizeParameterFlag.MuscleTone            => new CustomizeParameterValue(parameter.SkinColor.W),
            CustomizeParameterFlag.SkinSpecular          => new CustomizeParameterValue(parameter.SkinFresnelValue0),
            CustomizeParameterFlag.LipDiffuse            => new CustomizeParameterValue(parameter.LipColor),
            CustomizeParameterFlag.HairDiffuse           => new CustomizeParameterValue(parameter.MainColor),
            CustomizeParameterFlag.HairSpecular          => new CustomizeParameterValue(parameter.HairFresnelValue0),
            CustomizeParameterFlag.HairHighlight         => new CustomizeParameterValue(parameter.MeshColor),
            CustomizeParameterFlag.LeftEye               => new CustomizeParameterValue(parameter.LeftColor),
            CustomizeParameterFlag.RightEye              => new CustomizeParameterValue(parameter.RightColor),
            CustomizeParameterFlag.FeatureColor          => new CustomizeParameterValue(parameter.OptionColor),
            CustomizeParameterFlag.FacePaintUvMultiplier => new CustomizeParameterValue(parameter.LeftColor.W),
            CustomizeParameterFlag.FacePaintUvOffset     => new CustomizeParameterValue(parameter.RightColor.W),
            _                                            => CustomizeParameterValue.Zero,
        };

    public static Vector4 FromParameter(in DecalParameters parameter)
        => new CustomizeParameterValue(parameter.Color).InternalQuadruple;

    private static bool SetIfDifferent(ref Vector3 val, Vector3 @new)
    {
        if (@new == val)
            return false;

        val = @new;
        return true;
    }

    private static bool SetIfDifferent(ref float val, float @new)
    {
        if (@new == val)
            return false;

        val = @new;
        return true;
    }

    private static bool SetIfDifferent(ref Vector4 val, Vector4 @new)
    {
        if (@new == val)
            return false;

        val = @new;
        return true;
    }
}
