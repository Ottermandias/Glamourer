using Dalamud.Plugin;
using Glamourer.GameData;
using Newtonsoft.Json.Linq;
using OtterGui.Services;

namespace Glamourer.Interop.PalettePlus;

public class PaletteImport(DalamudPluginInterface pluginInterface) : IService
{
    private string ConfigFile
        => Path.Combine(Path.GetDirectoryName(pluginInterface.GetPluginConfigDirectory())!, "PalettePlus.json");

    public bool TryRead(string name, ref CustomizeParameterData data)
    {
        if (name.Length == 0)
            return false;


        var path = ConfigFile;
        if (!File.Exists(path))
            return false;

        try
        {
            var text     = File.ReadAllText(path);
            var obj      = JObject.Parse(text);
            var palettes = obj["SavedPalettes"];

            var target = palettes?.Children().FirstOrDefault(c => c["Name"]?.ToObject<string>() == name);
            if (target == null)
                return false;

            var conditions = target["Conditions"]?.ToObject<int>() ?? 0;
            var parameters = target["ShaderParams"];
            if (parameters == null)
                return false;

            var discard    = 0f;

            Parse("SkinTone",         ref data.SkinDiffuse.X,  ref data.SkinDiffuse.Y,  ref data.SkinDiffuse.Z,  ref discard);
            Parse("SkinGloss",        ref data.SkinSpecular.X, ref data.SkinSpecular.Y, ref data.SkinSpecular.Z, ref discard);
            Parse("LipColor",         ref data.LipDiffuse.X,   ref data.LipDiffuse.Y,   ref data.LipDiffuse.Z,   ref data.LipDiffuse.W);
            Parse("HairColor",        ref data.HairDiffuse.X,  ref data.HairDiffuse.Y,  ref data.HairDiffuse.Z,  ref discard);
            Parse("HairShine",        ref data.HairSpecular.X, ref data.HairSpecular.Y, ref data.HairSpecular.Z, ref discard);
            Parse("LeftEyeColor",     ref data.LeftEye.X,      ref data.LeftEye.Y,      ref data.LeftEye.Z,      ref discard);
            Parse("RaceFeatureColor", ref data.FeatureColor.X, ref data.FeatureColor.Y, ref data.FeatureColor.Z, ref discard);
            Parse("FacePaintColor",   ref data.DecalColor.X,   ref data.DecalColor.Y,   ref data.DecalColor.Z,   ref data.DecalColor.W);
            // Highlights is flag 2.
            if ((conditions & 2) == 2)
                Parse("HighlightsColor", ref data.HairHighlight.X, ref data.HairHighlight.Y, ref data.HairHighlight.Z, ref discard);
            // Heterochromia is flag 1
            if ((conditions & 1) == 1)
                Parse("RightEyeColor", ref data.RightEye.X, ref data.RightEye.Y, ref data.RightEye.Z, ref discard);

            ParseSingle("FacePaintOffset", ref data.FacePaintUvOffset);
            ParseSingle("FacePaintWidth",  ref data.FacePaintUvMultiplier);
            ParseSingle("MuscleTone",      ref data.MuscleTone);

            return true;

            void Parse(string attribute, ref float x, ref float y, ref float z, ref float w)
            {
                var node = parameters![attribute];
                if (node == null)
                    return;

                var xVal = node["X"]?.ToObject<float>();
                var yVal = node["Y"]?.ToObject<float>();
                var zVal = node["Z"]?.ToObject<float>();
                var wVal = node["W"]?.ToObject<float>();
                if (xVal.HasValue)
                    x = xVal.Value > 0 ? MathF.Sqrt(xVal.Value) : -MathF.Sqrt(-xVal.Value);
                if (yVal.HasValue)
                    y = yVal.Value > 0 ? MathF.Sqrt(yVal.Value) : -MathF.Sqrt(-yVal.Value);
                if (zVal.HasValue)
                    z = zVal.Value > 0 ? MathF.Sqrt(zVal.Value) : -MathF.Sqrt(-zVal.Value);
                if (wVal.HasValue)
                    w = wVal.Value;
            }

            void ParseSingle(string attribute, ref float value)
            {
                var node = parameters![attribute]?.ToObject<float>();
                if (node.HasValue)
                    value = node.Value;
            }
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not read Palette+ configuration:\n{ex}");
            return false;
        }
    }
}
