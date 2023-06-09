using System;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.Util;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui;

public partial class Interface
{
    private class DebugDataTab
    {
        private readonly CustomizationService _service;

        public DebugDataTab(CustomizationService service)
            => _service = service;

        public void Draw()
        {
            if (!_service.Valid)
                return;

            using var tab = ImRaii.TabItem("Debug");
            if (!tab)
                return;

            foreach (var clan in _service.AwaitedService.Clans)
            {
                foreach (var gender in _service.AwaitedService.Genders)
                    DrawCustomizationInfo(_service.AwaitedService.GetList(clan, gender));
            }
        }

        public void DrawCustomizationInfo(CustomizationSet set)
        {
            if (!ImGui.CollapsingHeader($"{CustomizeExtensions.ClanName(_service.AwaitedService, set.Clan, set.Gender)} {set.Gender}"))
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
