using System;
using System.Linq;
using System.Text;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.Utility;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DebugTab;

public class DesignConverterPanel(DesignConverter _designConverter) : IDebugTabTree
{
    public string Label
        => "Design Converter";

    public bool Disabled
        => false;

    private string      _clipboardText    = string.Empty;
    private byte[]      _clipboardData    = [];
    private byte[]      _dataUncompressed = [];
    private byte        _version          = 0;
    private string      _textUncompressed = string.Empty;
    private JObject?    _json             = null;
    private DesignBase? _tmpDesign        = null;
    private Exception?  _clipboardProblem = null;

    public void Draw()
    {
        if (ImGui.Button("Import Clipboard"))
        {
            _clipboardText    = string.Empty;
            _clipboardData    = [];
            _dataUncompressed = [];
            _textUncompressed = string.Empty;
            _json             = null;
            _tmpDesign        = null;
            _clipboardProblem = null;

            try
            {
                _clipboardText = ImGui.GetClipboardText();
                _clipboardData = Convert.FromBase64String(_clipboardText);
                _version       = _clipboardData[0];
                if (_version == 5)
                    _clipboardData = _clipboardData[DesignBase64Migration.Base64SizeV4..];
                _version          = _clipboardData.Decompress(out _dataUncompressed);
                _textUncompressed = Encoding.UTF8.GetString(_dataUncompressed);
                _json             = JObject.Parse(_textUncompressed);
                _tmpDesign        = _designConverter.FromBase64(_clipboardText, true, true, out _);
            }
            catch (Exception ex)
            {
                _clipboardProblem = ex;
            }
        }

        if (_clipboardText.Length > 0)
        {
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(_clipboardText);
        }

        if (_clipboardData.Length > 0)
        {
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(string.Join(" ", _clipboardData.Select(b => b.ToString("X2"))));
        }

        if (_dataUncompressed.Length > 0)
        {
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(string.Join(" ", _dataUncompressed.Select(b => b.ToString("X2"))));
        }

        if (_textUncompressed.Length > 0)
        {
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(_textUncompressed);
        }

        if (_json != null)
            ImGui.TextUnformatted("JSON Parsing Successful!");

        if (_tmpDesign != null)
            DesignManagerPanel.DrawDesign(_tmpDesign, null);

        if (_clipboardProblem != null)
        {
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(_clipboardProblem.ToString());
        }
    }
}
