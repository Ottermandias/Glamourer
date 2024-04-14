using Glamourer.Api.Enums;
using Glamourer.Designs;
using ImGuiNET;
using OtterGui;
using static Penumbra.GameData.Files.ShpkFile;

namespace Glamourer.Gui.Tabs.DebugTab.IpcTester;

public static class IpcTesterHelpers
{
    public static void DrawFlagInput(ref ApplyFlag flags)
    {
        var value = (flags & ApplyFlag.Once) != 0;
        if (ImGui.Checkbox("Apply Once", ref value))
            flags = value ? flags | ApplyFlag.Once : flags & ~ApplyFlag.Once;

        ImGui.SameLine();
        value = (flags & ApplyFlag.Equipment) != 0;
        if (ImGui.Checkbox("Apply Equipment", ref value))
            flags = value ? flags | ApplyFlag.Equipment : flags & ~ApplyFlag.Equipment;

        ImGui.SameLine();
        value = (flags & ApplyFlag.Customization) != 0;
        if (ImGui.Checkbox("Apply Customization", ref value))
            flags = value ? flags | ApplyFlag.Customization : flags & ~ApplyFlag.Customization;

        ImGui.SameLine();
        value = (flags & ApplyFlag.Lock) != 0;
        if (ImGui.Checkbox("Lock Actor", ref value))
            flags = value ? flags | ApplyFlag.Lock : flags & ~ApplyFlag.Lock;
    }

    public static void IndexInput(ref int index)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        ImGui.InputInt("Game Object Index", ref index, 0, 0);
    }

    public static void KeyInput(ref uint key)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        var keyI = (int)key;
        if (ImGui.InputInt("Key", ref keyI, 0, 0))
            key = (uint)keyI;
    }

    public static void NameInput(ref string name)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##gameObject", "Character Name...", ref name, 64);
    }

    public static void DrawIntro(string intro)
    {
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(intro);
        ImGui.TableNextColumn();
    }
}
