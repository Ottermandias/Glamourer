using Glamourer.Config;
using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.SettingsTab;

public sealed class DesignColorUi(DesignColors colors, Configuration config) : IUiService
{
    private string _newName = string.Empty;

    public void Draw()
    {
        using var table = Im.Table.Begin("designColors"u8, 3, TableFlags.RowBackground);
        if (!table)
            return;

        var     changeString = string.Empty;
        Rgba32? changeValue  = null;

        table.SetupColumn("##Delete"u8,   TableColumnFlags.WidthFixed, Im.Style.FrameHeight);
        table.SetupColumn("##Select"u8,   TableColumnFlags.WidthFixed, Im.Style.FrameHeight);
        table.SetupColumn("Color Name"u8, TableColumnFlags.WidthStretch);

        table.HeaderRow();

        table.NextColumn();
        if (ImEx.Icon.Button(LunaStyle.RefreshIcon, "Revert the color used for missing design colors to its default."u8,
                colors.MissingColor == DesignColors.MissingColorDefault))
        {
            changeString = DesignColors.MissingColorName;
            changeValue  = DesignColors.MissingColorDefault;
        }

        table.NextColumn();
        if (DrawColorButton(DesignColors.MissingColorNameU8, colors.MissingColor, out var newColor))
        {
            changeString = DesignColors.MissingColorName;
            changeValue  = newColor;
        }

        table.NextColumn();
        Im.Cursor.X += Im.Style.FramePadding.X;
        Im.Text(DesignColors.MissingColorNameU8);
        Im.Tooltip.OnHover("This color is used when the color specified in a design is not available."u8);

        var disabled = !config.DeleteDesignModifier.IsActive();
        foreach (var (idx, (name, color)) in colors.Index())
        {
            using var id = Im.Id.Push(idx);
            table.NextColumn();

            if (ImEx.Icon.Button(LunaStyle.DeleteIcon, "Delete this color. This does not remove it from designs using it."u8, disabled))
            {
                changeString = name;
                changeValue  = null;
            }

            if (disabled)
                Im.Tooltip.OnHover($"\nHold {config.DeleteDesignModifier} to delete.");

            table.NextColumn();
            if (DrawColorButton(name, color, out newColor))
            {
                changeString = name;
                changeValue  = newColor;
            }

            table.NextColumn();
            Im.Cursor.X += Im.Style.FramePadding.X;
            Im.Text(name);
        }

        table.NextColumn();
        (var tt, disabled) = _newName.Length == 0
            ? ("Specify a name for a new color first.", true)
            : _newName is DesignColors.MissingColorName or DesignColors.AutomaticName
                ? ($"You can not use the name {DesignColors.MissingColorName} or {DesignColors.AutomaticName}, choose a different one.", true)
                : colors.ContainsKey(_newName)
                    ? ($"The color {_newName} already exists, please choose a different name.", true)
                    : ($"Add a new color {_newName} to your list.", false);
        if (ImEx.Icon.Button(LunaStyle.AddObjectIcon, tt, disabled))
        {
            changeString = _newName;
            changeValue  = 0xFFFFFFFF;
        }

        table.NextColumn();
        table.NextColumn();
        Im.Item.SetNextWidth(Im.ContentRegion.Available.X);
        if (Im.Input.Text("##newDesignColor"u8, ref _newName, "New Color Name..."u8, InputTextFlags.EnterReturnsTrue))
        {
            changeString = _newName;
            changeValue  = 0xFFFFFFFF;
        }

        if (changeString.Length > 0)
        {
            if (!changeValue.HasValue)
                colors.DeleteColor(changeString);
            else
                colors.SetColor(changeString, changeValue.Value);
        }
    }

    public static bool DrawColorButton(Utf8StringHandler<LabelStringHandlerBuffer> tooltip, Rgba32 color, out Rgba32 newColor)
    {
        var ret = Im.Color.Editor(tooltip, ref color, ColorEditorFlags.AlphaPreviewHalf | ColorEditorFlags.NoInputs);
        Im.Tooltip.OnHover(ref tooltip);
        newColor = color;
        return ret;
    }
}
