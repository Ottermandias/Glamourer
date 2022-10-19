using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Glamourer.Gui.Customization;
using ImGuiNET;
using OtterGui.Raii;

namespace Glamourer.Gui;

internal partial class Interface : Window, IDisposable
{
    private readonly Glamourer _plugin;

    private readonly ActorTab      _actorTab;
    private readonly DebugStateTab _debugStateTab;
    private readonly DebugDataTab  _debugDataTab;

    public Interface(Glamourer plugin)
        : base(GetLabel())
    {
        _plugin                                              =  plugin;
        Dalamud.PluginInterface.UiBuilder.DisableGposeUiHide =  true;
        Dalamud.PluginInterface.UiBuilder.OpenConfigUi       += Toggle;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(675, 675),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        _actorTab      = new ActorTab(_plugin.CurrentManipulations);
        _debugStateTab = new DebugStateTab(_plugin.CurrentManipulations);
        _debugDataTab  = new DebugDataTab(Glamourer.Customization);
    }

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("##Tabs");
        if (!tabBar)
            return;

        try
        {
            UpdateState();

            _actorTab.Draw();
            DrawSettingsTab();
            _debugStateTab.Draw();
            _debugDataTab.Draw();
            //        DrawSaves();
            //        DrawFixedDesignsTab();
            //        DrawRevertablesTab();
        }
        catch (Exception e)
        {
            PluginLog.Error($"Unexpected Error during Draw:\n{e}");
        }
    }

    public void Dispose()
    {
        Dalamud.PluginInterface.UiBuilder.OpenConfigUi -= Toggle;
        CustomizationDrawer.Dispose();
    }

    private static string GetLabel()
        => Glamourer.Version.Length == 0
            ? "Glamourer###GlamourerConfigWindow"
            : $"Glamourer v{Glamourer.Version}###GlamourerConfigWindow";
}

//public const     float  SelectorWidth  = 200;
//public const     float  MinWindowWidth = 675;
//public const     int    GPoseObjectId  = 201;
//private const    string PluginName     = "Glamourer";
//private readonly string _glamourerHeader;
//
//private readonly IReadOnlyDictionary<byte, Stain>                                       _stains;
//private readonly IReadOnlyDictionary<uint, ModelCeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeehara>                                  _models;
//private readonly IObjectIdentifier                                                      _identifier;
//private readonly Dictionary<EquipSlot, (ComboWithFilter<Item>, ComboWithFilter<Stain>)> _combos;
//private readonly ImGuiScene.TextureWrap?                                                _legacyTattooIcon;
//private readonly Dictionary<EquipSlot, string>                                          _equipSlotNames;
//private readonly DesignManager                                                          _designs;
//private readonly Glamourer                                                              _plugin;
//
//private bool _visible;
//private bool _inGPose;
//
//public Interface(Glamourer plugin)
//{
//    _plugin  = plugin;
//    _designs = plugin.Designs;
//    _glamourerHeader = Glamourer.Version.Length > 0
//        ? $"{PluginName} v{Glamourer.Version}###{PluginName}Main"
//        : $"{PluginName}###{PluginName}Main";
//    Dalamud.PluginInterface.UiBuilder.DisableGposeUiHide =  true;
//    Dalamud.PluginInterface.UiBuilder.Draw               += Draw;
//    Dalamud.PluginInterface.UiBuilder.OpenConfigUi       += ToggleVisibility;
//
//    _equipSlotNames = GetEquipSlotNames();
//
//    _stains     = GameData.Stains(Dalamud.GameData);
//    _models     = GameData.Models(Dalamud.GameData);
//    _identifier = Penumbra.GameData.GameData.GetIdentifier(Dalamud.GameData, Dalamud.ClientState.ClientLanguage);
//
//
//    var stainCombo = CreateDefaultStainCombo(_stains.Values.ToArray());
//
//    var equip = GameData.ItemsBySlot(Dalamud.GameData);
//    _combos           = equip.ToDictionary(kvp => kvp.Key, kvp => CreateCombos(kvp.Key, kvp.Value, stainCombo));
//    _legacyTattooIcon = GetLegacyTattooIcon();
//}
//
//public void ToggleVisibility()
//    => _visible = !_visible;
//
//
//private void Draw()
//{
//    if (!_visible)
//        return;
//
//    ImGui.SetNextWindowSizeConstraints(Vector2.One * MinWindowWidth * ImGui.GetIO().FontGlobalScale,
//        Vector2.One * 5000 * ImGui.GetIO().FontGlobalScale);
//    if (!ImGui.Begin(_glamourerHeader, ref _visible))
//    {
//        ImGui.End();
//        return;
//    }
//
//    try
//    {
//        using var tabBar = ImRaii.TabBar("##tabBar");
//        if (!tabBar)
//            return;
//
//        _inGPose           = Dalamud.Objects[GPoseObjectId] != null;
//        _iconSize          = Vector2.One * ImGui.GetTextLineHeightWithSpacing() * 2;
//        _actualIconSize    = _iconSize + 2 * ImGui.GetStyle().FramePadding;
//        _comboSelectorSize = 4 * _actualIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
//        _percentageSize    = _comboSelectorSize;
//        _inputIntSize      = 2 * _actualIconSize.X + ImGui.GetStyle().ItemSpacing.X;
//        _raceSelectorWidth = _inputIntSize + _percentageSize - _actualIconSize.X;
//        _itemComboWidth    = 6 * _actualIconSize.X + 4 * ImGui.GetStyle().ItemSpacing.X - ColorButtonWidth + 1;
//
//        DrawPlayerTab();
//        DrawSaves();
//        DrawFixedDesignsTab();
//        DrawConfigTab();
//        DrawRevertablesTab();
//    }
//    finally
//    {
//        ImGui.End();
//    }
//}
