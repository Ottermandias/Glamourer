using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using Glamourer.GameData;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer(
    ITextureProvider textures,
    CustomizeService _service,
    Configuration _config,
    FavoriteManager _favorites,
    HeightService _heightService)
    : IDisposable
{
    private readonly Vector4              _redTint      = new(0.6f, 0.3f, 0.3f, 1f);
    private readonly IDalamudTextureWrap? _legacyTattoo = GetLegacyTattooIcon(textures);

    private Exception? _terminate;

    private CustomizeArray _customize = CustomizeArray.Default;
    private CustomizeSet   _set       = null!;

    public CustomizeArray Customize
        => _customize;

    public CustomizeFlag Changed     { get; private set; }
    public CustomizeFlag ChangeApply { get; private set; }

    private CustomizeFlag _initialApply;
    private bool          _locked;
    private bool          _lockedRedraw;
    private Vector2       _spacing;
    private Vector2       _iconSize;
    private Vector2       _framedIconSize;
    private float         _inputIntSize;
    private float         _inputIntSizeNoButtons;
    private float         _comboSelectorSize;
    private float         _raceSelectorWidth;
    private bool          _withApply;

    public void Dispose()
        => _legacyTattoo?.Dispose();

    public bool Draw(CustomizeArray current, bool locked, bool lockedRedraw)
    {
        _withApply = false;
        Init(current, locked, lockedRedraw);

        return DrawInternal();
    }

    public bool Draw(CustomizeArray current, CustomizeFlag apply, bool locked, bool lockedRedraw)
    {
        ChangeApply   = apply;
        _initialApply = apply;
        _withApply    = !_config.HideApplyCheckmarks;
        Init(current, locked, lockedRedraw);
        return DrawInternal();
    }

    private void Init(CustomizeArray current, bool locked, bool lockedRedraw)
    {
        UpdateSizes();
        _terminate    = null;
        Changed       = 0;
        _customize    = current;
        _locked       = locked;
        _lockedRedraw = lockedRedraw;
    }

    // Set state for drawing of current customization.
    private CustomizeIndex _currentIndex;
    private CustomizeFlag  _currentFlag;
    private CustomizeValue _currentByte = CustomizeValue.Zero;
    private bool           _currentApply;
    private int            _currentCount;
    private string         _currentOption = string.Empty;

    // Prepare a new customization option.
    private ImRaii.Id SetId(CustomizeIndex index)
    {
        _currentIndex  = index;
        _currentFlag   = index.ToFlag();
        _currentApply  = ChangeApply.HasFlag(_currentFlag);
        _currentByte   = _customize[index];
        _currentCount  = _set.Count(index, _customize.Face);
        _currentOption = _set.Option(index);
        return ImRaii.PushId((int)index);
    }

    // Update the current id with a new value.
    private void UpdateValue(CustomizeValue value)
    {
        if (_currentByte == value)
            return;

        // Hrothgar Face Hack.
        if (_currentIndex is CustomizeIndex.Face && _set.Race is Race.Hrothgar)
            value += 4;

        _customize[_currentIndex] =  value;
        Changed                   |= _currentFlag;
    }

    private bool DrawInternal()
    {
        using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, _spacing);

        try
        {
            DrawRaceGenderSelector();
            DrawBodyType();

            _set = _service.Manager.GetSet(_customize.Clan, _customize.Gender);

            foreach (var id in _set.Order[MenuType.Percentage])
                PercentageSelector(id);

            Functions.IteratePairwise(_set.Order[MenuType.IconSelector], DrawIconSelector, ImGui.SameLine);

            DrawMultiIconSelector();

            foreach (var id in _set.Order[MenuType.ListSelector])
                DrawListSelector(id, false);

            foreach (var id in _set.Order[MenuType.List1Selector])
                DrawListSelector(id, true);

            Functions.IteratePairwise(_set.Order[MenuType.ColorPicker], DrawColorPicker, ImGui.SameLine);

            Functions.IteratePairwise(_set.Order[MenuType.Checkmark], DrawCheckbox,
                () => ImGui.SameLine(_comboSelectorSize - _framedIconSize.X + ImGui.GetStyle().WindowPadding.X));
            return Changed != 0 || ChangeApply != _initialApply;
        }
        catch (Exception ex)
        {
            _terminate = ex;
            using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF4040FF);
            ImGui.NewLine();
            ImGuiUtil.TextWrapped(_terminate.ToString());
            return false;
        }
    }

    private void UpdateSizes()
    {
        _spacing               = ImGui.GetStyle().ItemSpacing with { X = ImGui.GetStyle().ItemInnerSpacing.X };
        _iconSize              = new Vector2(ImGui.GetTextLineHeight() * 2 + _spacing.Y + 2 * ImGui.GetStyle().FramePadding.Y);
        _framedIconSize        = _iconSize + 2 * ImGui.GetStyle().FramePadding;
        _inputIntSize          = 2 * _framedIconSize.X + 1 * _spacing.X;
        _inputIntSizeNoButtons = _inputIntSize - 2 * _spacing.X - 2 * ImGui.GetFrameHeight();
        _comboSelectorSize     = 4 * _framedIconSize.X + 3 * _spacing.X;
        _raceSelectorWidth     = _inputIntSize + _comboSelectorSize - _framedIconSize.X;
    }

    private static IDalamudTextureWrap? GetLegacyTattooIcon(ITextureProvider textures)
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Glamourer.LegacyTattoo.raw");
        if (resource == null)
            return null;

        var rawImage = new byte[resource.Length];
        var length   = resource.Read(rawImage, 0, (int)resource.Length);
        return length == resource.Length
            ? textures.CreateFromRaw(RawImageSpecification.Rgba32(192, 192), rawImage, "Glamourer.LegacyTattoo")
            : null;
    }
}
