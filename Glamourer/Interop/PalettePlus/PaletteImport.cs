using Dalamud.Plugin;
using Glamourer.Designs;
using Glamourer.GameData;
using Luna;
using Newtonsoft.Json.Linq;

namespace Glamourer.Interop.PalettePlus;

public class PaletteImport(IDalamudPluginInterface pluginInterface, DesignManager designManager, DesignFileSystem designFileSystem) : IService
{
    private string ConfigFile
        => Path.Combine(Path.GetDirectoryName(pluginInterface.GetPluginConfigDirectory())!, "PalettePlus.json");

    private readonly Dictionary<string, (CustomizeParameterData, CustomizeParameterFlag)> _data = [];

    public IReadOnlyDictionary<string, (CustomizeParameterData, CustomizeParameterFlag)> Data
    {
        get
        {
            if (_data.Count > 0)
                return _data;

            PopulateDict();
            return _data;
        }
    }

    public void ImportDesigns()
    {
        foreach (var (name, (palette, flags)) in Data)
        {
            var fullPath = $"PalettePlus/{name}";
            if (designFileSystem.Find(fullPath, out _))
            {
                Glamourer.Log.Information($"Skipped adding palette {name} because {fullPath} already exists.");
                continue;
            }

            var design = designManager.CreateEmpty(fullPath, true);
            design.Application = ApplicationCollection.None;
            foreach (var flag in flags.Iterate())
            {
                designManager.ChangeApplyParameter(design, flag, true);
                designManager.ChangeCustomizeParameter(design, flag, palette[flag]);
            }

            Glamourer.Log.Information($"Added design for palette {name} at {fullPath}.");
        }
    }

    private void PopulateDict()
    {
        var path = ConfigFile;
        if (!File.Exists(path))
            return;

        try
        {
            var text     = File.ReadAllText(path);
            var obj      = JObject.Parse(text);
            var palettes = obj["SavedPalettes"];
            if (palettes == null)
                return;

            foreach (var child in palettes.Children())
            {
                var name = child["Name"]?.ToObject<string>() ?? string.Empty;
                if (name.Length == 0)
                    continue;

                var conditions = child["Conditions"]?.ToObject<int>() ?? 0;
                var parameters = child["ShaderParams"];
                if (parameters == null)
                    continue;

                var orig    = name;
                var counter = 1;
                while (_data.ContainsKey(name))
                    name = $"{orig} #{++counter}";

                var                    data    = new CustomizeParameterData();
                CustomizeParameterFlag flags   = 0;
                var                    discard = 0f;
                Parse("SkinTone",           CustomizeParameterFlag.SkinDiffuse,
                    ref data.SkinDiffuse.X, ref data.SkinDiffuse.Y, ref data.SkinDiffuse.Z, ref discard);
                Parse("SkinGloss",           CustomizeParameterFlag.SkinSpecular,
                    ref data.SkinSpecular.X, ref data.SkinSpecular.Y, ref data.SkinSpecular.Z, ref discard);
                Parse("LipColor",          CustomizeParameterFlag.LipDiffuse,
                    ref data.LipDiffuse.X, ref data.LipDiffuse.Y, ref data.LipDiffuse.Z, ref data.LipDiffuse.W);
                Parse("HairColor",          CustomizeParameterFlag.HairDiffuse,
                    ref data.HairDiffuse.X, ref data.HairDiffuse.Y, ref data.HairDiffuse.Z, ref discard);
                Parse("HairShine",           CustomizeParameterFlag.HairSpecular,
                    ref data.HairSpecular.X, ref data.HairSpecular.Y, ref data.HairSpecular.Z, ref discard);
                Parse("LeftEyeColor",   CustomizeParameterFlag.LeftEye,
                    ref data.LeftEye.X, ref data.LeftEye.Y, ref data.LeftEye.Z, ref discard);
                Parse("RaceFeatureColor",    CustomizeParameterFlag.FeatureColor,
                    ref data.FeatureColor.X, ref data.FeatureColor.Y, ref data.FeatureColor.Z, ref discard);
                Parse("FacePaintColor",    CustomizeParameterFlag.DecalColor,
                    ref data.DecalColor.X, ref data.DecalColor.Y, ref data.DecalColor.Z, ref data.DecalColor.W);
                // Highlights is flag 2.
                if ((conditions & 2) == 2)
                    Parse("HighlightsColor",      CustomizeParameterFlag.HairHighlight,
                        ref data.HairHighlight.X, ref data.HairHighlight.Y, ref data.HairHighlight.Z, ref discard);
                // Heterochromia is flag 1
                if ((conditions & 1) == 1)
                {
                    Parse("RightEyeColor",   CustomizeParameterFlag.RightEye,
                        ref data.RightEye.X, ref data.RightEye.Y, ref data.RightEye.Z, ref discard);
                }
                else if (flags.HasFlag(CustomizeParameterFlag.LeftEye))
                {
                    data.RightEye =  data.LeftEye;
                    flags         |= CustomizeParameterFlag.RightEye;
                }

                ParseSingle("FacePaintOffset", CustomizeParameterFlag.FacePaintUvOffset,     ref data.FacePaintUvOffset);
                ParseSingle("FacePaintWidth",  CustomizeParameterFlag.FacePaintUvMultiplier, ref data.FacePaintUvMultiplier);
                ParseSingle("MuscleTone",      CustomizeParameterFlag.MuscleTone,            ref data.MuscleTone);

                while (!_data.TryAdd(name, (data, flags)))
                    name = $"{orig} ({++counter})";
                continue;


                void Parse(string attribute, CustomizeParameterFlag flag, ref float x, ref float y, ref float z, ref float w)
                {
                    var node = parameters![attribute];
                    if (node == null)
                        return;

                    flags |= flag;
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

                void ParseSingle(string attribute, CustomizeParameterFlag flag, ref float value)
                {
                    var node = parameters![attribute]?.ToObject<float>();
                    if (!node.HasValue)
                        return;

                    value =  node.Value;
                    flags |= flag;
                }
            }
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not read Palette+ configuration:\n{ex}");
        }
    }
}
