using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using Glamourer.Config;
using Glamourer.GameData;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer(
    ITextureProvider textures,
    CustomizeService service,
    Configuration config,
    FavoriteManager favorites,
    HeightService heightService)
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
        _withApply    = !config.HideApplyCheckmarks;
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
    private StringU8       _currentOption = StringU8.Empty;

    // Prepare a new customization option.
    private Im.IdDisposable SetId(CustomizeIndex index)
    {
        _currentIndex  = index;
        _currentFlag   = index.ToFlag();
        _currentApply  = ChangeApply.HasFlag(_currentFlag);
        _currentByte   = _customize[index];
        _currentCount  = _set.Count(index, _customize.Face);
        _currentOption = _set.Option(index);
        return Im.Id.Push((int)index);
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
        using var spacing = ImStyleDouble.ItemSpacing.Push(_spacing);

        try
        {
            DrawRaceGenderSelector();
            DrawBodyType();

            _set = service.Manager.GetSet(_customize.Clan, _customize.Gender);

            foreach (var id in _set.Order[MenuType.Percentage])
                PercentageSelector(id);

            foreach (var (i, icon) in _set.Order[MenuType.IconSelector].Index())
            {
                if ((i & 1) is 1)
                    Im.Line.Same();
                DrawIconSelector(icon);
            }

            DrawMultiIconSelector();

            foreach (var id in _set.Order[MenuType.ListSelector])
                DrawListSelector(id, false);

            foreach (var id in _set.Order[MenuType.List1Selector])
                DrawListSelector(id, true);

            foreach (var (i, color) in _set.Order[MenuType.ColorPicker].Index())
            {
                if ((i & 1) is 1)
                    Im.Line.Same();
                DrawColorPicker(color);
            }

            var offset = _comboSelectorSize - _framedIconSize.X + Im.Style.WindowPadding.X;
            foreach (var (i, check) in _set.Order[MenuType.Checkmark].Index())
            {
                if ((i & 1) is 1)
                    Im.Line.Same(offset);
                DrawCheckbox(check);
            }

            return Changed is not 0 || ChangeApply != _initialApply;
        }
        catch (Exception ex)
        {
            _terminate = ex;
            using var color = ImGuiColor.Text.Push(LunaStyle.ErrorBorderColor);
            Im.Line.New();
            Im.TextWrapped($"{_terminate}");
            return false;
        }
    }

    private void UpdateSizes()
    {
        _spacing               = Im.Style.ItemSpacing with { X = Im.Style.ItemInnerSpacing.X };
        _iconSize              = new Vector2(Im.Style.TextHeight * 2 + _spacing.Y + 2 * Im.Style.FramePadding.Y);
        _framedIconSize        = _iconSize + 2 * Im.Style.FramePadding;
        _inputIntSize          = 2 * _framedIconSize.X + 1 * _spacing.X;
        _inputIntSizeNoButtons = _inputIntSize - 2 * _spacing.X - 2 * Im.Style.FrameHeight;
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
