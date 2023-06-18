using System;
using System.Numerics;
using System.Reflection;
using Dalamud.Plugin;
using Glamourer.Customization;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer : IDisposable
{
    private readonly Vector4                 _redTint = new(0.6f, 0.3f, 0.3f, 1f);
    private readonly ImGuiScene.TextureWrap? _legacyTattoo;

    private bool       _withFlags = false;
    private Exception? _terminate = null;

    private Customize        _customize;
    private CustomizationSet _set = null!;

    public Customize Customize;

    public CustomizeFlag CurrentFlag { get; private set; }
    public CustomizeFlag Changed     { get; private set; }

    public bool RequiresRedraw
        => Changed.RequiresRedraw();

    private bool    _locked = false;
    private Vector2 _iconSize;
    private Vector2 _framedIconSize;
    private float   _inputIntSize;
    private float   _comboSelectorSize;
    private float   _raceSelectorWidth;

    private readonly CustomizationService _service;
    private readonly ItemManager          _items;

    public CustomizationDrawer(DalamudPluginInterface pi, CustomizationService service, ItemManager items)
    {
        _service      = service;
        _items        = items;
        _legacyTattoo = GetLegacyTattooIcon(pi);
        Customize     = Customize.Default;
    }

    public void Dispose()
    {
        _legacyTattoo?.Dispose();
    }

    public bool Draw(Customize current, bool locked)
    {
        _withFlags  = false;
        CurrentFlag = CustomizeFlagExtensions.All;
        Init(current, locked);
        return DrawInternal();
    }

    public bool Draw(Customize current, CustomizeFlag currentFlags, bool locked)
    {
        _withFlags  = true;
        CurrentFlag = currentFlags;
        Init(current, locked);
        return DrawInternal();
    }

    private void Init(Customize current, bool locked)
    {
        UpdateSizes();
        _terminate = null;
        Changed    = 0;
        _customize.Load(current);
        _locked = locked;
    }

    // Set state for drawing of current customization.
    private CustomizeIndex _currentIndex;
    private CustomizeFlag  _currentFlag;
    private CustomizeValue _currentByte = CustomizeValue.Zero;
    private int            _currentCount;
    private string         _currentOption = string.Empty;

    // Prepare a new customization option.
    private ImRaii.Id SetId(CustomizeIndex index)
    {
        _currentIndex  = index;
        _currentFlag   = index.ToFlag();
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

        _customize[_currentIndex] =  value;
        Changed                   |= _currentFlag;
    }

    private bool DrawInternal()
    {
        using var disabled = ImRaii.Disabled(_locked);

        try
        {
            DrawRaceGenderSelector();
            _set = _service.AwaitedService.GetList(_customize.Clan, _customize.Gender);

            foreach (var id in _set.Order[CharaMakeParams.MenuType.Percentage])
                PercentageSelector(id);

            CustomGui.IteratePairwise(_set.Order[CharaMakeParams.MenuType.IconSelector], DrawIconSelector, ImGui.SameLine);

            DrawMultiIconSelector();

            foreach (var id in _set.Order[CharaMakeParams.MenuType.ListSelector])
                DrawListSelector(id);

            CustomGui.IteratePairwise(_set.Order[CharaMakeParams.MenuType.ColorPicker], DrawColorPicker, ImGui.SameLine);

            CustomGui.IteratePairwise(_set.Order[CharaMakeParams.MenuType.Checkmark], DrawCheckbox,
                () => ImGui.SameLine(_inputIntSize + _framedIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X));
            return Changed != 0;
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
        _iconSize          = new Vector2(ImGui.GetTextLineHeightWithSpacing() * 2);
        _framedIconSize    = _iconSize + 2 * ImGui.GetStyle().FramePadding;
        _inputIntSize      = 2 * _framedIconSize.X + ImGui.GetStyle().ItemSpacing.X;
        _comboSelectorSize = 4 * _framedIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
        _raceSelectorWidth = _inputIntSize + _comboSelectorSize - _framedIconSize.X;
    }

    private static ImGuiScene.TextureWrap? GetLegacyTattooIcon(DalamudPluginInterface pi)
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Glamourer.LegacyTattoo.raw");
        if (resource == null)
            return null;

        var rawImage = new byte[resource.Length];
        var length   = resource.Read(rawImage, 0, (int)resource.Length);
        return length == resource.Length
            ? pi.UiBuilder.LoadImageRaw(rawImage, 192, 192, 4)
            : null;
    }
}
