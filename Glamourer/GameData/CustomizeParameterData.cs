using FFXIVClientStructs.FFXIV.Shader;

namespace Glamourer.GameData;

public struct CustomizeParameterData
{
    public Vector3 SkinDiffuse;
    public Vector3 SkinSpecular;
    public Vector3 LipDiffuse;
    public Vector3 HairDiffuse;
    public Vector3 HairSpecular;
    public Vector3 HairHighlight;
    public Vector3 LeftEye;
    public Vector3 RightEye;
    public Vector3 FeatureColor;
    public float   FacePaintUvMultiplier;
    public float   FacePaintUvOffset;
    public float   MuscleTone;
    public float   LipOpacity;

    public Vector3 this[CustomizeParameterFlag flag]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        readonly get
        {
            return flag switch
            {
                CustomizeParameterFlag.SkinDiffuse           => SkinDiffuse,
                CustomizeParameterFlag.MuscleTone            => new Vector3(MuscleTone, 0, 0),
                CustomizeParameterFlag.SkinSpecular          => SkinSpecular,
                CustomizeParameterFlag.LipDiffuse            => LipDiffuse,
                CustomizeParameterFlag.LipOpacity            => new Vector3(LipOpacity, 0, 0),
                CustomizeParameterFlag.HairDiffuse           => HairDiffuse,
                CustomizeParameterFlag.HairSpecular          => HairSpecular,
                CustomizeParameterFlag.HairHighlight         => HairHighlight,
                CustomizeParameterFlag.LeftEye               => LeftEye,
                CustomizeParameterFlag.RightEye              => RightEye,
                CustomizeParameterFlag.FeatureColor          => FeatureColor,
                CustomizeParameterFlag.FacePaintUvMultiplier => new Vector3(FacePaintUvMultiplier, 0, 0),
                CustomizeParameterFlag.FacePaintUvOffset     => new Vector3(FacePaintUvOffset,     0, 0),
                _                                            => Vector3.Zero,
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        set => Set(flag, value);
    }

    public bool Set(CustomizeParameterFlag flag, Vector3 value)
    {
        return flag switch
        {
            CustomizeParameterFlag.SkinDiffuse           => SetIfDifferent(ref SkinDiffuse,           value),
            CustomizeParameterFlag.MuscleTone            => SetIfDifferent(ref MuscleTone,            Math.Clamp(value[0], -100, 100)),
            CustomizeParameterFlag.SkinSpecular          => SetIfDifferent(ref SkinSpecular,          value),
            CustomizeParameterFlag.LipDiffuse            => SetIfDifferent(ref LipDiffuse,            value),
            CustomizeParameterFlag.LipOpacity            => SetIfDifferent(ref LipOpacity,            Math.Clamp(value[0], -100, 100)),
            CustomizeParameterFlag.HairDiffuse           => SetIfDifferent(ref HairDiffuse,           value),
            CustomizeParameterFlag.HairSpecular          => SetIfDifferent(ref HairSpecular,          value),
            CustomizeParameterFlag.HairHighlight         => SetIfDifferent(ref HairHighlight,         value),
            CustomizeParameterFlag.LeftEye               => SetIfDifferent(ref LeftEye,               value),
            CustomizeParameterFlag.RightEye              => SetIfDifferent(ref RightEye,              value),
            CustomizeParameterFlag.FeatureColor          => SetIfDifferent(ref FeatureColor,          value),
            CustomizeParameterFlag.FacePaintUvMultiplier => SetIfDifferent(ref FacePaintUvMultiplier, value[0]),
            CustomizeParameterFlag.FacePaintUvOffset     => SetIfDifferent(ref FacePaintUvOffset,     value[0]),
            _                                            => false,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public readonly void Apply(ref CustomizeParameter parameters, CustomizeParameterFlag flags = CustomizeParameterExtensions.All)
    {
        if (flags.HasFlag(CustomizeParameterFlag.SkinDiffuse))
            parameters.SkinColor = Convert(SkinDiffuse, parameters.SkinColor.W);
        if (flags.HasFlag(CustomizeParameterFlag.MuscleTone))
            parameters.SkinColor.W = MuscleTone;
        if (flags.HasFlag(CustomizeParameterFlag.SkinSpecular))
            parameters.SkinFresnelValue0 = Convert(SkinSpecular, 0);
        if (flags.HasFlag(CustomizeParameterFlag.LipDiffuse))
            parameters.LipColor = Convert(LipDiffuse, parameters.LipColor.W);
        if (flags.HasFlag(CustomizeParameterFlag.LipOpacity))
            parameters.LipColor.W = LipOpacity;
        if (flags.HasFlag(CustomizeParameterFlag.HairDiffuse))
            parameters.MainColor = Convert(HairDiffuse);
        if (flags.HasFlag(CustomizeParameterFlag.HairSpecular))
            parameters.HairFresnelValue0 = Convert(HairSpecular);
        if (flags.HasFlag(CustomizeParameterFlag.HairHighlight))
            parameters.MeshColor = Convert(HairHighlight);
        if (flags.HasFlag(CustomizeParameterFlag.LeftEye))
            parameters.LeftColor = Convert(LeftEye, parameters.LeftColor.W);
        if (flags.HasFlag(CustomizeParameterFlag.RightEye))
            parameters.RightColor = Convert(RightEye, parameters.RightColor.W);
        if (flags.HasFlag(CustomizeParameterFlag.FeatureColor))
            parameters.OptionColor = Convert(FeatureColor);
        if (flags.HasFlag(CustomizeParameterFlag.FacePaintUvMultiplier))
            parameters.LeftColor.W = FacePaintUvMultiplier;
        if (flags.HasFlag(CustomizeParameterFlag.FacePaintUvOffset))
            parameters.RightColor.W = FacePaintUvOffset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public readonly void ApplySingle(ref CustomizeParameter parameters, CustomizeParameterFlag flag)
    {
        switch (flag)
        {
            case CustomizeParameterFlag.SkinDiffuse:
                parameters.SkinColor = Convert(SkinDiffuse, parameters.SkinColor.W);
                break;
            case CustomizeParameterFlag.MuscleTone:
                parameters.SkinColor.W = MuscleTone;
                break;
            case CustomizeParameterFlag.SkinSpecular:
                parameters.SkinFresnelValue0 = Convert(SkinSpecular, 0);
                break;
            case CustomizeParameterFlag.LipDiffuse:
                parameters.LipColor = Convert(LipDiffuse, parameters.LipColor.W);
                break;
            case CustomizeParameterFlag.LipOpacity:
                parameters.LipColor.W = LipOpacity;
                break;
            case CustomizeParameterFlag.HairDiffuse:
                parameters.MainColor = Convert(HairDiffuse);
                break;
            case CustomizeParameterFlag.HairSpecular:
                parameters.HairFresnelValue0 = Convert(HairSpecular);
                break;
            case CustomizeParameterFlag.HairHighlight:
                parameters.MeshColor = Convert(HairHighlight);
                break;
            case CustomizeParameterFlag.LeftEye:
                parameters.LeftColor = Convert(LeftEye, parameters.LeftColor.W);
                break;
            case CustomizeParameterFlag.RightEye:
                parameters.RightColor = Convert(RightEye, parameters.RightColor.W);
                break;
            case CustomizeParameterFlag.FeatureColor:
                parameters.OptionColor = Convert(FeatureColor);
                break;
            case CustomizeParameterFlag.FacePaintUvMultiplier:
                parameters.LeftColor.W = FacePaintUvMultiplier;
                break;
            case CustomizeParameterFlag.FacePaintUvOffset:
                parameters.RightColor.W = FacePaintUvOffset;
                break;
        }
    }

    public static CustomizeParameterData FromParameters(in CustomizeParameter parameter)
        => new()
        {
            FacePaintUvOffset     = parameter.RightColor.W,
            FacePaintUvMultiplier = parameter.LeftColor.W,
            MuscleTone            = parameter.SkinColor.W,
            LipOpacity            = parameter.LipColor.W,
            SkinDiffuse           = Convert(parameter.SkinColor),
            SkinSpecular          = Convert(parameter.SkinFresnelValue0),
            LipDiffuse            = Convert(parameter.LipColor),
            HairDiffuse           = Convert(parameter.MainColor),
            HairSpecular          = Convert(parameter.HairFresnelValue0),
            HairHighlight         = Convert(parameter.MeshColor),
            LeftEye               = Convert(parameter.LeftColor),
            RightEye              = Convert(parameter.RightColor),
            FeatureColor          = Convert(parameter.OptionColor),
        };

    public static Vector3 FromParameter(in CustomizeParameter parameter, CustomizeParameterFlag flag)
        => flag switch
        {
            CustomizeParameterFlag.SkinDiffuse           => Convert(parameter.SkinColor),
            CustomizeParameterFlag.MuscleTone            => new Vector3(parameter.SkinColor.W),
            CustomizeParameterFlag.SkinSpecular          => Convert(parameter.SkinFresnelValue0),
            CustomizeParameterFlag.LipDiffuse            => Convert(parameter.LipColor),
            CustomizeParameterFlag.LipOpacity            => new Vector3(parameter.LipColor.W),
            CustomizeParameterFlag.HairDiffuse           => Convert(parameter.MainColor),
            CustomizeParameterFlag.HairSpecular          => Convert(parameter.HairFresnelValue0),
            CustomizeParameterFlag.HairHighlight         => Convert(parameter.MeshColor),
            CustomizeParameterFlag.LeftEye               => Convert(parameter.LeftColor),
            CustomizeParameterFlag.RightEye              => Convert(parameter.RightColor),
            CustomizeParameterFlag.FeatureColor          => Convert(parameter.OptionColor),
            CustomizeParameterFlag.FacePaintUvMultiplier => new Vector3(parameter.LeftColor.W),
            CustomizeParameterFlag.FacePaintUvOffset     => new Vector3(parameter.RightColor.W),
            _                                            => Vector3.Zero,
        };

    private static FFXIVClientStructs.FFXIV.Common.Math.Vector4 Convert(Vector3 value, float w)
        => new(value.X * value.X, value.Y * value.Y, value.Z * value.Z, w);

    private static Vector3 Convert(FFXIVClientStructs.FFXIV.Common.Math.Vector3 value)
        => new((float)Math.Sqrt(value.X), (float)Math.Sqrt(value.Y), (float)Math.Sqrt(value.Z));

    private static Vector3 Convert(FFXIVClientStructs.FFXIV.Common.Math.Vector4 value)
        => new((float)Math.Sqrt(value.X), (float)Math.Sqrt(value.Y), (float)Math.Sqrt(value.Z));

    private static FFXIVClientStructs.FFXIV.Common.Math.Vector3 Convert(Vector3 value)
        => new(value.X * value.X, value.Y * value.Y, value.Z * value.Z);

    private static bool SetIfDifferent(ref Vector3 val, Vector3 @new)
    {
        @new.X = Math.Clamp(@new.X, 0, 1);
        @new.Y = Math.Clamp(@new.Y, 0, 1);
        @new.Z = Math.Clamp(@new.Z, 0, 1);

        if (@new == val)
            return false;

        val = @new;
        return true;
    }

    private static bool SetIfDifferent<T>(ref T val, T @new) where T : IEqualityOperators<T, T, bool>
    {
        if (@new == val)
            return false;

        val = @new;
        return true;
    }
}
