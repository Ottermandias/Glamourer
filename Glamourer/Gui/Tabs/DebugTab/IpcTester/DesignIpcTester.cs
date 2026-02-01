using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using Dalamud.Bindings.ImGui;
using ImSharp;
using Luna;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;

namespace Glamourer.Gui.Tabs.DebugTab.IpcTester;

public class DesignIpcTester(IDalamudPluginInterface pluginInterface) : IUiService
{
    private Dictionary<Guid, string> _designs = [];
    private int                      _gameObjectIndex;
    private string                   _gameObjectName = string.Empty;
    private string                   _designName     = string.Empty;
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
        ImUtf8.InputText("##designName"u8, ref _designName, "Design Name..."u8);
        ImGuiUtil.GuidInput("##identifier", "Design Identifier...", string.Empty, ref _design, ref _designText,
            Im.ContentRegion.Available.X);
        IpcTesterHelpers.DrawFlagInput(ref _flags);

        using var table = Im.Table.Begin("##table"u8, 2, TableFlags.SizingFixedFit);

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

        IpcTesterHelpers.DrawIntro(GetExtendedDesignData.Label);
        if (_design.HasValue)
        {
            var (display, path, color, draw) = new GetExtendedDesignData(pluginInterface).Invoke(_design.Value);
            if (path.Length > 0)
                ImUtf8.Text($"{display} ({path}){(draw ? " in QDB"u8 : ""u8)}", color);
            else
                ImUtf8.Text("No Data"u8);
        }
        else
        {
            ImUtf8.Text("No Data"u8);
        }

        IpcTesterHelpers.DrawIntro(GetDesignBase64.Label);
        if (ImUtf8.Button("To Clipboard##Base64"u8) && _design.HasValue)
        {
            var data = new GetDesignBase64(pluginInterface).Invoke(_design.Value);
            ImUtf8.SetClipboardText(data);
        }

        IpcTesterHelpers.DrawIntro(AddDesign.Label);
        if (ImUtf8.Button("Add from Clipboard"u8))
            try
            {
                var data = ImUtf8.GetClipboardText();
                _lastError = new AddDesign(pluginInterface).Invoke(data, _designName, out var newDesign);
                if (_lastError is GlamourerApiEc.Success)
                {
                    _design     = newDesign;
                    _designText = newDesign.ToString();
                }
            }
            catch
            {
                _lastError = GlamourerApiEc.UnknownError;
            }

        IpcTesterHelpers.DrawIntro(DeleteDesign.Label);
        if (ImUtf8.Button("Delete##Design"u8) && _design.HasValue)
            _lastError = new DeleteDesign(pluginInterface).Invoke(_design.Value);
    }

    private void DrawDesignsPopup()
    {
        ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(500, 500));
        using var p = ImRaii.Popup("Designs");
        if (!p)
            return;

        using var table = Im.Table.Begin("Designs"u8, 2, TableFlags.SizingFixedFit);
        foreach (var (guid, name) in _designs)
        {
            ImGuiUtil.DrawTableColumn(name);
            using var f = Im.Font.PushMono();
            ImGui.TableNextColumn();
            ImGuiUtil.CopyOnClickSelectable(guid.ToString());
        }

        if (ImGui.Button("Close", -Vector2.UnitX) || !ImGui.IsWindowFocused())
            ImGui.CloseCurrentPopup();
    }
}
