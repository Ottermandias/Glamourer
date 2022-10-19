using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Glamourer.Customization;
using Glamourer.Interop;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Customization;

internal partial class CustomizationDrawer
{
    private static readonly Vector4                 RedTint = new(0.6f, 0.3f, 0.3f, 1f);
    private static readonly ImGuiScene.TextureWrap? LegacyTattoo;

    private readonly Vector2 _iconSize;
    private readonly Vector2 _framedIconSize;
    private readonly float   _inputIntSize;
    private readonly float   _comboSelectorSize;
    private readonly float   _raceSelectorWidth;

    private Customize                  _customize;
    private CharacterEquip             _equip;
    private IReadOnlyCollection<Actor> _actors = Array.Empty<Actor>();
    private CustomizationSet           _set    = null!;

    private CustomizationDrawer()
    {
        _iconSize          = new Vector2(ImGui.GetTextLineHeightWithSpacing() * 2);
        _framedIconSize    = _iconSize + 2 * ImGui.GetStyle().FramePadding;
        _inputIntSize      = 2 * _framedIconSize.X + ImGui.GetStyle().ItemSpacing.X;
        _comboSelectorSize = 4 * _framedIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
        _raceSelectorWidth = _inputIntSize + _comboSelectorSize - _framedIconSize.X;
    }

    static CustomizationDrawer()
        => LegacyTattoo = GetLegacyTattooIcon();

    public static void Dispose()
        => LegacyTattoo?.Dispose();

    private static ImGuiScene.TextureWrap? GetLegacyTattooIcon()
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Glamourer.LegacyTattoo.raw");
        if (resource == null)
            return null;

        var rawImage = new byte[resource.Length];
        var length   = resource.Read(rawImage, 0, (int)resource.Length);
        return length == resource.Length
            ? Dalamud.PluginInterface.UiBuilder.LoadImageRaw(rawImage, 192, 192, 4)
            : null;
    }

    public static void Draw(Customize customize, CharacterEquip equip, IReadOnlyCollection<Actor> actors, bool locked)
    {
        var d = new CustomizationDrawer()
        {
            _customize = customize,
            _equip     = equip,
            _actors    = actors,
        };


        if (!ImGui.CollapsingHeader("Character Customization"))
            return;

        using var disabled = ImRaii.Disabled(locked);

        d.DrawRaceGenderSelector();

        d._set = Glamourer.Customization.GetList(customize.Clan, customize.Gender);

        foreach (var id in d._set.Order[CharaMakeParams.MenuType.Percentage])
            d.PercentageSelector(id);

        Functions.IteratePairwise(d._set.Order[CharaMakeParams.MenuType.IconSelector], d.DrawIconSelector, ImGui.SameLine);

        d.DrawMultiIconSelector();

        foreach (var id in d._set.Order[CharaMakeParams.MenuType.ListSelector])
            d.DrawListSelector(id);

        Functions.IteratePairwise(d._set.Order[CharaMakeParams.MenuType.ColorPicker], d.DrawColorPicker, ImGui.SameLine);

        Functions.IteratePairwise(d._set.Order[CharaMakeParams.MenuType.Checkmark], d.DrawCheckbox,
            () => ImGui.SameLine(d._inputIntSize + d._framedIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X));
    }

    public static void Draw(Customize customize, IReadOnlyCollection<Actor> actors, bool locked = false)
        => Draw(customize, CharacterEquip.Null, actors, locked);

    public static void Draw(Customize customize, CharacterEquip equip, bool locked = false)
        => Draw(customize, equip, Array.Empty<Actor>(), locked);

    public static void Draw(Customize customize, bool locked = false)
        => Draw(customize, CharacterEquip.Null, Array.Empty<Actor>(), locked);

    // Set state for drawing of current customization.
    private CustomizeIndex _currentIndex;
    private CustomizeValue _currentByte = CustomizeValue.Zero;
    private int            _currentCount;
    private string         _currentOption = string.Empty;

    // Prepare a new customization option.
    private ImRaii.Id SetId(CustomizeIndex index)
    {
        _currentIndex  = index;
        _currentByte   = _customize[index];
        _currentCount  = _set.Count(index, _customize.Face);
        _currentOption = _set.Option(index);
        return ImRaii.PushId((int)index);
    }

    // Update the current id with a value,
    // also update actors if any.
    private void UpdateValue(CustomizeValue value)
    {
        if (_customize[_currentIndex] == value)
            return;

        _customize[_currentIndex] = value;
        UpdateActors();
    }

    // Update all relevant Actors by calling the UpdateCustomize game function.
    private void UpdateActors()
    {
        foreach (var actor in _actors)
            Glamourer.RedrawManager.UpdateCustomize(actor, _customize);
    }
}
