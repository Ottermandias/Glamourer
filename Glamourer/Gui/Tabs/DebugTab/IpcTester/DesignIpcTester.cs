using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;

namespace Glamourer.Gui.Tabs.DebugTab.IpcTester;

public class DesignIpcTester(DalamudPluginInterface pluginInterface) : IUiService
{
    private Dictionary<Guid, string> _designs = [];
    private int                      _gameObjectIndex;
    private string                   _gameObjectName = string.Empty;
    private uint                     _key;
    private ApplyFlag                _flags = ApplyFlagEx.DesignDefault;
    private Guid?                    _design;
    private string                   _designText = string.Empty;
    private GlamourerApiEc           _lastError;

    public void Draw()
    {
        using var tree = ImRaii.TreeNode("Designs");
        if (!tree)
            return;

        IpcTesterHelpers.IndexInput(ref _gameObjectIndex);
        IpcTesterHelpers.KeyInput(ref _key);
        IpcTesterHelpers.NameInput(ref _gameObjectName);
        ImGuiUtil.GuidInput("##identifier", "Design Identifier...", string.Empty, ref _design, ref _designText,
            ImGui.GetContentRegionAvail().X);
        IpcTesterHelpers.DrawFlagInput(ref _flags);

        using var table = ImRaii.Table("##table", 2, ImGuiTableFlags.SizingFixedFit);

        IpcTesterHelpers.DrawIntro("Last Error");
        ImGui.TextUnformatted(_lastError.ToString());

        IpcTesterHelpers.DrawIntro(GetDesignList.Label);
        DrawDesignsPopup();
        if (ImGui.Button("Get##Designs"))
        {
            _designs = new GetDesignList(pluginInterface).Invoke();
            ImGui.OpenPopup("Designs");
        }

        IpcTesterHelpers.DrawIntro(ApplyDesign.Label);
        if (ImGuiUtil.DrawDisabledButton("Apply##Idx", Vector2.Zero, string.Empty, !_design.HasValue))
            _lastError = new ApplyDesign(pluginInterface).Invoke(_design!.Value, _gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(ApplyDesignName.Label);
        if (ImGuiUtil.DrawDisabledButton("Apply##Name", Vector2.Zero, string.Empty, !_design.HasValue))
            _lastError = new ApplyDesignName(pluginInterface).Invoke(_design!.Value, _gameObjectName, _key, _flags);
    }

    private void DrawDesignsPopup()
    {
        ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(500, 500));
        using var p = ImRaii.Popup("Designs");
        if (!p)
            return;

        using var table = ImRaii.Table("Designs", 2, ImGuiTableFlags.SizingFixedFit);
        foreach (var (guid, name) in _designs)
        {
            ImGuiUtil.DrawTableColumn(name);
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGui.TableNextColumn();
            ImGuiUtil.CopyOnClickSelectable(guid.ToString());
        }

        if (ImGui.Button("Close", -Vector2.UnitX) || !ImGui.IsWindowFocused())
            ImGui.CloseCurrentPopup();
    }
}
