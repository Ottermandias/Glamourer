using Dalamud.Interface.Utility;
using Glamourer.Interop.Penumbra;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGui.Text;
using OtterGui.Widgets;
using MouseWheelType = OtterGui.Widgets.MouseWheelType;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class ModCombo : FilterComboCache<(Mod Mod, ModSettings Settings, int Count)>
{
    public ModCombo(PenumbraService penumbra, Logger log, DesignFileSystemSelector selector)
        : base(() => penumbra.GetMods(selector.Selected?.FilteredItemNames.ToArray() ?? []), MouseWheelType.None, log)
        => SearchByParts = false;

    protected override string ToString((Mod Mod, ModSettings Settings, int Count) obj)
        => obj.Mod.Name;

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => filter.IsContained(Items[globalIndex].Mod.Name) || filter.IsContained(Items[globalIndex].Mod.DirectoryName);

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        using var id = ImUtf8.PushId(globalIdx);
        var (mod, settings, count) = Items[globalIdx];
        bool ret;
        var color = settings.Enabled
            ? count > 0
                ? ColorId.ContainsItemsEnabled.Value()
                : ImGuiColor.Text.Get()
            : count > 0
                ? ColorId.ContainsItemsDisabled.Value()
                : ImGuiColor.TextDisabled.Get();
        using (ImGuiColor.Text.Push(color))
        {
            ret = ImUtf8.Selectable(mod.Name, selected);
        }

        if (ImGui.IsItemHovered())
        {
            using var style          = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * Im.Style.GlobalScale);
            using var tt             = ImUtf8.Tooltip();
            var       namesDifferent = mod.Name != mod.DirectoryName;
            Im.Dummy(new Vector2(300 * Im.Style.GlobalScale, 0));
            using (ImUtf8.Group())
            {
                if (namesDifferent)
                    ImUtf8.Text("Directory Name"u8);
                ImUtf8.Text("Enabled"u8);
                ImUtf8.Text("Priority"u8);
                ImUtf8.Text("Affected Design Items"u8);
                DrawSettingsLeft(settings);
            }

            ImGui.SameLine(Math.Max(ImGui.GetItemRectSize().X + 3 * Im.Style.ItemSpacing.X, 150 * Im.Style.GlobalScale));
            using (ImUtf8.Group())
            {
                if (namesDifferent)
                    ImUtf8.Text(mod.DirectoryName);
                ImUtf8.Text($"{settings.Enabled}");
                ImUtf8.Text($"{settings.Priority}");
                ImUtf8.Text($"{count}");
                DrawSettingsRight(settings);
            }
        }

        return ret;
    }

    public static void DrawSettingsLeft(ModSettings settings)
    {
        foreach (var setting in settings.Settings)
        {
            ImUtf8.Text(setting.Key);
            for (var i = 1; i < setting.Value.Count; ++i)
                Im.Line.New();
        }
    }

    public static void DrawSettingsRight(ModSettings settings)
    {
        foreach (var setting in settings.Settings)
        {
            if (setting.Value.Count == 0)
                ImUtf8.Text("<None Enabled>"u8);
            else
                foreach (var option in setting.Value)
                    ImUtf8.Text(option);
        }
    }
}
