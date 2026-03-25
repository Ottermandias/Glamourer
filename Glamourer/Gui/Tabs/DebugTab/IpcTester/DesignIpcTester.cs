using Dalamud.Plugin;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using ImSharp;
using Luna;

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
    private GlamourerApiEc           _lastError;

    public void Draw()
    {
        using var tree = Im.Tree.Node("Designs"u8);
        if (!tree)
            return;

        IpcTesterHelpers.IndexInput(ref _gameObjectIndex);
        IpcTesterHelpers.KeyInput(ref _key);
        IpcTesterHelpers.NameInput(ref _gameObjectName);
        Im.Input.Text("##designName"u8, ref _designName, "Design Name..."u8);
        ImEx.GuidInput("##identifier"u8, ref _design, Im.ContentRegion.Available.X);
        IpcTesterHelpers.DrawFlagInput(ref _flags);

        using var table = Im.Table.Begin("##table"u8, 2, TableFlags.SizingFixedFit);

        IpcTesterHelpers.DrawIntro("Last Error"u8);
        Im.Text($"{_lastError}");

        IpcTesterHelpers.DrawIntro(GetDesignList.LabelU8);
        DrawDesignsPopup();
        if (Im.Button("Get##Designs"u8))
        {
            _designs = new GetDesignList(pluginInterface).Invoke();
            Im.Popup.Open("Designs"u8);
        }

        IpcTesterHelpers.DrawIntro(ApplyDesign.LabelU8);
        if (ImEx.Button("Apply##Idx"u8, Vector2.Zero, StringU8.Empty, !_design.HasValue))
            _lastError = new ApplyDesign(pluginInterface).Invoke(_design!.Value, _gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(ApplyDesignName.LabelU8);
        if (ImEx.Button("Apply##Name"u8, Vector2.Zero, StringU8.Empty, !_design.HasValue))
            _lastError = new ApplyDesignName(pluginInterface).Invoke(_design!.Value, _gameObjectName, _key, _flags);

        IpcTesterHelpers.DrawIntro(GetExtendedDesignData.LabelU8);
        if (_design.HasValue)
        {
            var (display, path, color, draw) = new GetExtendedDesignData(pluginInterface).Invoke(_design.Value);
            if (path.Length > 0)
                Im.Text($"{display} ({path}){(draw ? " in QDB"u8 : ""u8)}", color);
            else
                Im.Text("No Data"u8);
        }
        else
        {
            Im.Text("No Data"u8);
        }

        IpcTesterHelpers.DrawIntro(GetDesignBase64.LabelU8);
        if (Im.Button("To Clipboard##Base64"u8) && _design.HasValue)
        {
            var data = new GetDesignBase64(pluginInterface).Invoke(_design.Value);
            if (data is not null)
                Im.Clipboard.Set(data);
        }

        IpcTesterHelpers.DrawIntro(AddDesign.LabelU8);
        if (Im.Button("Add from Clipboard"u8))
            try
            {
                var data = Im.Clipboard.GetUtf16();
                _lastError = new AddDesign(pluginInterface).Invoke(data, _designName, out var newDesign);
                if (_lastError is GlamourerApiEc.Success)
                    _design = newDesign;
            }
            catch
            {
                _lastError = GlamourerApiEc.UnknownError;
            }

        IpcTesterHelpers.DrawIntro(DeleteDesign.LabelU8);
        if (Im.Button("Delete##Design"u8) && _design.HasValue)
            _lastError = new DeleteDesign(pluginInterface).Invoke(_design.Value);
    }

    private void DrawDesignsPopup()
    {
        Im.Window.SetNextSize(ImEx.ScaledVector(500, 500));
        using var p = Im.Popup.Begin("Designs"u8);
        if (!p)
            return;

        using var table = Im.Table.Begin("Designs"u8, 2, TableFlags.SizingFixedFit);
        foreach (var (guid, name) in _designs)
        {
            table.DrawColumn(name);
            using var f = Im.Font.PushMono();
            table.NextColumn();
            ImEx.CopyOnClickSelectable($"{guid}");
        }

        if (Im.Button("Close"u8, Im.ContentRegion.Available with { Y = 0 }) || !Im.Window.Focused())
            Im.Popup.CloseCurrent();
    }
}
