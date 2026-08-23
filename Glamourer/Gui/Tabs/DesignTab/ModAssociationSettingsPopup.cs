using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.Api.Enums;

namespace Glamourer.Gui.Tabs.DesignTab;

/// <summary>
/// A side window, modelled after the advanced dye editor, that lets the user configure the
/// Penumbra option-group selections stored in a design's mod association. Changes are persisted
/// to the design and applied to Penumbra as temporary settings (live preview).
/// </summary>
public sealed class ModAssociationSettingsPopup(PenumbraService penumbra, DesignManager manager, Configuration config) : IService
{
    public static readonly AwesomeIcon EditIcon = LunaStyle.EditIcon;

    private Design?     _design;
    private Mod?        _mod;
    private ModSettings _settings = ModSettings.Empty;
    private IReadOnlyDictionary<string, (string[] Options, GroupType Type)>? _groups;
    private bool        _forceFocus;

    /// <summary> Draw the toggle button that opens/closes the settings window for a given association row. </summary>
    public void DrawButton(Design design, Mod mod, ModSettings settings)
    {
        var isOpen = _mod == mod && ReferenceEquals(_design, design);
        using (ImStyleBorder.Frame.Push(ColorId.HeaderButtons.Value(), 2 * Im.Style.GlobalScale, isOpen))
        {
            if (ImEx.Icon.Button(EditIcon, "Configure this mod's option settings and apply them to Penumbra temporarily."u8,
                    !penumbra.Available))
            {
                if (isOpen)
                    Close();
                else
                    Open(design, mod, settings);
            }
        }
    }

    /// <summary> Draw the window itself if it is open. Closes automatically if the selected design changed. </summary>
    public void Draw(Design currentDesign)
    {
        if (_mod is not { } mod || _design is null)
            return;

        if (!ReferenceEquals(_design, currentDesign) || !penumbra.Available)
        {
            Close();
            return;
        }

        DrawWindow(mod);
    }

    private void Open(Design design, Mod mod, ModSettings settings)
    {
        _design = design;
        _mod    = mod;
        _groups = penumbra.GetAvailableSettings(mod);

        // Baseline the working copy from the mod's current effective selections in Penumbra, so that
        // groups the design does not explicitly store still show their real state (as in Penumbra's own
        // panel). The design's stored selections are then overlaid on top and take precedence.
        var merged = new Dictionary<string, List<string>>();
        var live   = penumbra.GetModSettings(mod, out _);
        foreach (var (group, options) in live.Settings)
            merged[group] = [.. options];
        foreach (var (group, options) in settings.Settings)
            merged[group] = [.. options];

        _settings   = settings with { Settings = merged };
        _forceFocus = true;
    }

    private void Close()
    {
        _design   = null;
        _mod      = null;
        _groups   = null;
        _settings = ModSettings.Empty;
    }

    private void DrawWindow(Mod mod)
    {
        var flags = WindowFlags.NoFocusOnAppearing
          | WindowFlags.NoCollapse
          | WindowFlags.NoDocking;

        var parentPos  = Im.Window.Position;
        var parentSize = Im.Window.Size;
        var width      = 320 * Im.Style.GlobalScale;

        if (config.KeepModSettingsAttached)
        {
            // Dock to the right of the main window and lock it in place.
            Im.Window.SetNextPosition(new Vector2(parentPos.X + parentSize.X + Im.Style.WindowPadding.X, parentPos.Y));
            Im.Window.SetNextSize(new Vector2(width, parentSize.Y));
            flags |= WindowFlags.NoMove;
        }
        else
        {
            // Free-floating: only size/position it on first appearance, then let the user move it.
            Im.Window.SetNextSize(new Vector2(width, parentSize.Y), Condition.FirstUseEver);
        }

        using var window = Im.Window.Begin("Mod Settings###GlamourerModAssociationSettings"u8, flags);
        if (Im.Window.Appearing || _forceFocus)
        {
            Im.Window.SetFocus("###GlamourerModAssociationSettings"u8);
            _forceFocus = false;
        }

        if (window)
            DrawContent(mod);
    }

    private void DrawContent(Mod mod)
    {
        if (ImEx.Icon.Button(LunaStyle.CancelIcon, "Close this window."u8))
        {
            Close();
            return;
        }

        Im.Line.Same();
        ImEx.TextFrameAligned(mod.Name);
        Im.Tooltip.OnHover($"Mod Directory:    {mod.DirectoryName}");
        Im.Separator();

        if (_groups is null)
        {
            Im.Text("Could not read this mod's options from Penumbra (is it still installed?)."u8);
            return;
        }

        if (_groups.Count is 0)
        {
            Im.Text("This mod has no configurable option groups."u8);
            return;
        }

        var idx = 0;
        foreach (var (groupName, (options, type)) in _groups)
        {
            using var id = Im.Id.Push(idx++);
            DrawGroup(mod, groupName, options, type);
            Im.Separator();
        }
    }

    private void DrawGroup(Mod mod, string groupName, string[] options, GroupType type)
    {
        switch (type)
        {
            case GroupType.Single:
                DrawSingleGroup(mod, groupName, options);
                break;
            case GroupType.Multi:
            case GroupType.Imc:
            case GroupType.Combining:
                DrawMultiGroup(mod, groupName, options);
                break;
            default:
                DrawComplexGroup(groupName);
                break;
        }
    }

    private void DrawSingleGroup(Mod mod, string groupName, string[] options)
    {
        _settings.Settings.TryGetValue(groupName, out var selected);
        var current = selected is { Count: > 0 } ? selected[0] : string.Empty;

        ImEx.TextFrameAligned(groupName);
        Im.Item.SetNextWidthFull();
        using var combo = Im.Combo.Begin($"##{groupName}", current.Length > 0 ? current : "<Default>");
        if (!combo)
            return;

        foreach (var option in options)
        {
            if (Im.Selectable(option, option == current) && option != current)
            {
                _settings.Settings[groupName] = [option];
                Commit(mod);
            }
        }
    }

    private void DrawMultiGroup(Mod mod, string groupName, string[] options)
    {
        ImEx.TextFrameAligned(groupName);
        if (!_settings.Settings.TryGetValue(groupName, out var selected))
        {
            selected                      = [];
            _settings.Settings[groupName] = selected;
        }

        using var indent = Im.Indent();
        foreach (var option in options)
        {
            var enabled = selected.Contains(option);
            if (!Im.Checkbox(option, ref enabled))
                continue;

            if (enabled)
                selected.Add(option);
            else
                selected.Remove(option);
            Commit(mod);
        }
    }

    private void DrawComplexGroup(string groupName)
    {
        _settings.Settings.TryGetValue(groupName, out var selected);
        var value = selected is { Count: > 0 } ? string.Join(", ", selected) : "<Default>";
        ImEx.TextFrameAligned($"{groupName}: {value}");
        Im.Tooltip.OnHover(
            "This group uses an advanced type (Combining/Complex) that can't be edited here yet.\nConfigure it in Penumbra, then use the Refresh button on the association row to capture it."u8);
    }

    /// <summary> Persist the current working settings to the design and live-apply them to Penumbra. </summary>
    private void Commit(Mod mod)
    {
        if (_design is null)
            return;

        manager.UpdateMod(_design, mod, _settings);
        var text = penumbra.SetMod(mod, _settings, StateSource.Manual, false);
        if (text.Length > 0)
            Glamourer.Messager.NotificationMessage(text, NotificationType.Warning, false);
    }
}
