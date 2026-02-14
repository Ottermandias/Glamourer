using Glamourer.Designs;
using Glamourer.GameData;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.NpcTab;

public sealed class NpcSelection : IUiService, IDisposable
{
    private readonly LocalNpcAppearanceData _data;
    private readonly DesignColors           _colors;
    private readonly DesignConverter        _converter;

    public NpcSelection(LocalNpcAppearanceData data, DesignColors colors, DesignConverter converter)
    {
        _data      = data;
        _colors    = colors;
        _converter = converter;

        _data.DataChanged    += OnDataChanged;
        _colors.ColorChanged += OnColorChanged;
    }

    private void OnColorChanged()
    {
        if (!HasSelection)
            return;

        Color = _data.GetData(Data).Color.ToVector();
    }

    private void OnDataChanged()
    {
        if (!HasSelection)
            return;

        ColorText             = _data.GetColor(Data);
        ColorTextU8           = ColorText.Length is 0 ? DesignColors.AutomaticNameU8 : new StringU8(ColorText);
        (var color, Favorite) = _data.GetData(Data);
        Color                 = color.ToVector();
    }

    public NpcData  Data { get; private set; }
    public StringU8 Name { get; private set; } = StringU8.Empty;

    public bool     Favorite    { get; private set; } = false;
    public string   ColorText   { get; private set; } = string.Empty;
    public StringU8 ColorTextU8 { get; private set; } = DesignColors.AutomaticNameU8;
    public Vector4  Color       { get; private set; }

    public uint Id
        => Data.Id;

    public bool HasSelection
        => Id is not 0;

    public DesignData ToDesignData()
    {
        var items      = _converter.FromDrawData(Data.Equip(), Data.Mainhand, Data.Offhand, true).ToArray();
        var designData = new DesignData { Customize = Data.Customize };
        foreach (var (slot, item, stain) in items)
        {
            designData.SetItem(slot, item);
            designData.SetStain(slot, stain);
        }

        return designData;
    }

    public DesignBase ToDesignBase()
    {
        var data = ToDesignData();
        return _converter.Convert(data, new StateMaterialManager(), ApplicationRules.NpcFromModifiers());
    }

    public string ToBase64()
    {
        var data = ToDesignData();
        return _converter.ShareBase64(data, new StateMaterialManager(), ApplicationRules.NpcFromModifiers());
    }

    public void Update(in NpcCacheItem item)
    {
        Data        = item.Npc;
        Name        = item.Name.Utf8;
        Favorite    = item.Favorite;
        ColorText   = item.ColorText;
        ColorTextU8 = ColorText.Length > 0 ? new StringU8(ColorText) : DesignColors.AutomaticNameU8;
        Color       = item.Color;
    }

    public void Dispose()
    {
        _data.DataChanged    -= OnDataChanged;
        _colors.ColorChanged -= OnColorChanged;
    }
}
