using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Actors;
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
        public const     int    GPoseActorId   = 201;
        private const    string PluginName     = "Glamourer";
        private readonly string _glamourerHeader;

        private readonly IReadOnlyDictionary<byte, Stain>                                       _stains;
        private readonly ActorTable                                                             _actors;
        private readonly IObjectIdentifier                                                      _identifier;
        private readonly Dictionary<EquipSlot, (ComboWithFilter<Item>, ComboWithFilter<Stain>)> _combos;
        private readonly ImGuiScene.TextureWrap?                                                _legacyTattooIcon;
        private readonly Dictionary<EquipSlot, string>                                          _equipSlotNames;
        private readonly DesignManager                                                          _designs;
        private readonly Glamourer                                                              _plugin;

        private bool _visible = false;
        private bool _inGPose = false;

        public Interface(Glamourer plugin)
        {
            _plugin  = plugin;
            _designs = plugin.Designs;
            _glamourerHeader = Glamourer.Version.Length > 0
                ? $"{PluginName} v{Glamourer.Version}###{PluginName}Main"
                : $"{PluginName}###{PluginName}Main";
            Glamourer.PluginInterface.UiBuilder.DisableGposeUiHide =  true;
            Glamourer.PluginInterface.UiBuilder.OnBuildUi          += Draw;
            Glamourer.PluginInterface.UiBuilder.OnOpenConfigUi     += ToggleVisibility;

            _equipSlotNames = GetEquipSlotNames();

            _stains     = GameData.Stains(Glamourer.PluginInterface);
            _identifier = Penumbra.GameData.GameData.GetIdentifier(Glamourer.PluginInterface);
            _actors     = Glamourer.PluginInterface.ClientState.Actors;

            var stainCombo = CreateDefaultStainCombo(_stains.Values.ToArray());

            var equip = GameData.ItemsBySlot(Glamourer.PluginInterface);
            _combos           = equip.ToDictionary(kvp => kvp.Key, kvp => CreateCombos(kvp.Key, kvp.Value, stainCombo));
            _legacyTattooIcon = GetLegacyTattooIcon();
        }

        public void ToggleVisibility(object _, object _2)
            => _visible = !_visible;

        public void Dispose()
        {
            _legacyTattooIcon?.Dispose();
            Glamourer.PluginInterface.UiBuilder.OnBuildUi      -= Draw;
            Glamourer.PluginInterface.UiBuilder.OnOpenConfigUi -= ToggleVisibility;
        }

        private void Draw()
        {
            if (!_visible)
                return;

            ImGui.SetNextWindowSizeConstraints(Vector2.One * MinWindowWidth * ImGui.GetIO().FontGlobalScale,
                Vector2.One * 5000 * ImGui.GetIO().FontGlobalScale);
            if (!ImGui.Begin(_glamourerHeader, ref _visible))
                return;

            try
            {
                using var raii = new ImGuiRaii();
                if (!raii.Begin(() => ImGui.BeginTabBar("##tabBar"), ImGui.EndTabBar))
                    return;

                _inGPose           = _actors[GPoseActorId] != null;
                _iconSize          = Vector2.One * ImGui.GetTextLineHeightWithSpacing() * 2;
                _actualIconSize    = _iconSize + 2 * ImGui.GetStyle().FramePadding;
                _comboSelectorSize = 4 * _actualIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
                _percentageSize    = _comboSelectorSize;
                _inputIntSize      = 2 * _actualIconSize.X + ImGui.GetStyle().ItemSpacing.X;
                _raceSelectorWidth = _inputIntSize + _percentageSize - _actualIconSize.X;
                _itemComboWidth    = 6 * _actualIconSize.X + 4 * ImGui.GetStyle().ItemSpacing.X - ColorButtonWidth + 1;

                DrawActorTab();
                DrawSaves();
                DrawConfigTab();
            }
            finally
            {
                ImGui.End();
            }
        }
    }
}
