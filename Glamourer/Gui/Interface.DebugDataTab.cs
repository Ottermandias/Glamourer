using System;
using Glamourer.Customization;
using Glamourer.Util;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui;

internal partial class Interface
{
    private class DebugDataTab
    {
        private readonly ICustomizationManager _mg;

        public DebugDataTab(ICustomizationManager manager)
            => _mg = manager;

        public void Draw()
        {
            using var tab = ImRaii.TabItem("Debug");
            if (!tab)
                return;

            foreach (var clan in _mg.Clans)
            {
                foreach (var gender in _mg.Genders)
                    DrawCustomizationInfo(_mg.GetList(clan, gender));
            }
        }

        public static void DrawCustomizationInfo(CustomizationSet set)
        {
            if (!ImGui.CollapsingHeader($"{CustomizeExtensions.ClanName(set.Clan, set.Gender)} {set.Gender}"))
                return;

            using var table = ImRaii.Table("data", 5);
            if (!table)
                return;

            foreach (var index in Enum.GetValues<CustomizeIndex>())
            {
                ImGuiUtil.DrawTableColumn(index.ToString());
                ImGuiUtil.DrawTableColumn(set.Option(index));
                ImGuiUtil.DrawTableColumn(set.IsAvailable(index) ? "Available" : "Unavailable");
                ImGuiUtil.DrawTableColumn(set.Type(index).ToString());
                ImGuiUtil.DrawTableColumn(set.Count(index).ToString());
            }
        }
    }
}
