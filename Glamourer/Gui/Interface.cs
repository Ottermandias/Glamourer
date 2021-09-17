using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Glamourer.Designs;
using ImGuiNET;
using Penumbra.GameData;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui
{
    internal partial class Interface : IDisposable
    {
        public const     float  SelectorWidth  = 200;
        public const     float  MinWindowWidth = 675;
        public const     int    GPoseObjectId   = 201;
        private const    string PluginName     = "Glamourer";
        private readonly string _glamourerHeader;

        private readonly IReadOnlyDictionary<byte, Stain>                                       _stains;
        private readonly IObjectIdentifier                                                      _identifier;
        private readonly Dictionary<EquipSlot, (ComboWithFilter<Item>, ComboWithFilter<Stain>)> _combos;
        private readonly ImGuiScene.TextureWrap?                                                _legacyTattooIcon;
        private readonly Dictionary<EquipSlot, string>                                          _equipSlotNames;
        private readonly DesignManager                                                          _designs;
        private readonly Glamourer                                                              _plugin;

        private bool _visible;
        private bool _inGPose;

        public Interface(Glamourer plugin)
        {
            _plugin  = plugin;
            _designs = plugin.Designs;
            _glamourerHeader = Glamourer.Version.Length > 0
                ? $"{PluginName} v{Glamourer.Version}###{PluginName}Main"
                : $"{PluginName}###{PluginName}Main";
            Dalamud.PluginInterface.UiBuilder.DisableGposeUiHide =  true;
            Dalamud.PluginInterface.UiBuilder.Draw               += Draw;
            Dalamud.PluginInterface.UiBuilder.OpenConfigUi       += ToggleVisibility;

            _equipSlotNames = GetEquipSlotNames();

            _stains     = GameData.Stains(Dalamud.GameData);
            _identifier = Penumbra.GameData.GameData.GetIdentifier(Dalamud.GameData, Dalamud.ClientState.ClientLanguage);

            var stainCombo = CreateDefaultStainCombo(_stains.Values.ToArray());

            var equip = GameData.ItemsBySlot(Dalamud.GameData);
            _combos           = equip.ToDictionary(kvp => kvp.Key, kvp => CreateCombos(kvp.Key, kvp.Value, stainCombo));
            _legacyTattooIcon = GetLegacyTattooIcon();
        }

        public void ToggleVisibility()
            => _visible = !_visible;

        public void Dispose()
        {
            _legacyTattooIcon?.Dispose();
            Dalamud.PluginInterface.UiBuilder.Draw         -= Draw;
            Dalamud.PluginInterface.UiBuilder.OpenConfigUi -= ToggleVisibility;
        }

        private void Draw()
        {
            if (!_visible)
                return;

            ImGui.SetNextWindowSizeConstraints(Vector2.One * MinWindowWidth * ImGui.GetIO().FontGlobalScale,
                Vector2.One * 5000 * ImGui.GetIO().FontGlobalScale);
            if (!ImGui.Begin(_glamourerHeader, ref _visible))
            {
                ImGui.End();
                return;
            }

            try
            {
                using var raii = new ImGuiRaii();
                if (!raii.Begin(() => ImGui.BeginTabBar("##tabBar"), ImGui.EndTabBar))
                    return;

                _inGPose           = Dalamud.Objects[GPoseObjectId] != null;
                _iconSize          = Vector2.One * ImGui.GetTextLineHeightWithSpacing() * 2;
                _actualIconSize    = _iconSize + 2 * ImGui.GetStyle().FramePadding;
                _comboSelectorSize = 4 * _actualIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
                _percentageSize    = _comboSelectorSize;
                _inputIntSize      = 2 * _actualIconSize.X + ImGui.GetStyle().ItemSpacing.X;
                _raceSelectorWidth = _inputIntSize + _percentageSize - _actualIconSize.X;
                _itemComboWidth    = 6 * _actualIconSize.X + 4 * ImGui.GetStyle().ItemSpacing.X - ColorButtonWidth + 1;

                DrawPlayerTab();
                DrawSaves();
                DrawFixedDesignsTab();
                DrawConfigTab();
            }
            finally
            {
                ImGui.End();
            }
        }
    }
}
