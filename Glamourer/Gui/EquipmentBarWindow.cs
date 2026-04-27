using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game;
using Glamourer.Config;
using Glamourer.Gui.Equipment;
using Glamourer.Gui.Materials;
using Glamourer.Gui.Tabs.ActorTab;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui;

public class EquipmentBarWindow : Window, IDisposable
{
    private WindowFlags GetFlags
        => _config.Ephemeral.LockEquipmentBar
            ? WindowFlags.NoDecoration | WindowFlags.NoDocking | WindowFlags.NoFocusOnAppearing | WindowFlags.NoMove
            : WindowFlags.NoDecoration | WindowFlags.NoDocking | WindowFlags.NoFocusOnAppearing;

    private readonly Configuration    _config;
    private readonly ActorSelection   _selection;
    private readonly StateManager     _stateManager;
    private readonly EquipmentDrawer  _equipmentDrawer;
    private readonly MainWindow       _mainWindow;
    private readonly ActorPanel       _actorPanel;
    private readonly AdvancedDyePopup _advancedDyes;

    private readonly Im.ColorStyleDisposable _style = new();

    public EquipmentBarWindow(Configuration config, ActorSelection selection, StateManager stateManager, EquipmentDrawer equipmentDrawer,
        MainWindow mainWindow, ActorPanel actorPanel, AdvancedDyePopup advancedDyes)
        : base("Glamourer Equipment Bar", WindowFlags.NoDecoration | WindowFlags.NoDocking)
    {
        _config          = config;
        _selection       = selection;
        _stateManager    = stateManager;
        _equipmentDrawer = equipmentDrawer;
        _mainWindow      = mainWindow;
        _actorPanel      = actorPanel;
        _advancedDyes    = advancedDyes;

        mainWindow.Open             += OnMainWindowOpen;
        actorPanel.OpenEquipmentBar += OnActorPanelOpenEqpBar;

        Size               = Vector2.Zero;
        RespectCloseHotkey = false;
    }

    public void Dispose()
    {
        _mainWindow.Open             -= OnMainWindowOpen;
        _actorPanel.OpenEquipmentBar -= OnActorPanelOpenEqpBar;
    }

    private void OnActorPanelOpenEqpBar()
        => IsOpen = true;

    private void OnMainWindowOpen()
        => IsOpen = false;

    public override void OnOpen()
        => _mainWindow.IsOpen = false;

    public override bool DrawConditions()
        => _selection.State is not null;

    public override void PreDraw()
    {
        Flags = GetFlags;

        base.PreDraw();

        _style.Push(ImStyleDouble.WindowPadding, new Vector2(Im.Style.GlobalScale * 4))
            .Push(ImStyleSingle.WindowBorderThickness, 0);
        _style.Push(ImGuiColor.WindowBackground, ColorId.QuickDesignBg.Value())
            .Push(ImGuiColor.Button,          ColorId.QuickDesignButton.Value())
            .Push(ImGuiColor.FrameBackground, ColorId.QuickDesignFrame.Value());
    }

    public override void PostDraw()
        => _style.Dispose();

    public override void Draw()
    {
        var buttonWidth = new Vector2(Im.Style.FrameHeight * 4.0f + Im.Style.ItemInnerSpacing.X * 3.0f, Im.Style.FrameHeight);
        ImEx.TextFramed(_selection.ShortName, buttonWidth,
            textColor: _selection.Data.Valid ? ColorId.ActorAvailable.Value() : ColorId.ActorUnavailable.Value(),
            frameColor: ImGuiColor.Button.Get());
        if (ImEx.Icon.LabeledButton(FontAwesomeIcon.TheaterMasks.Icon(), "Expand"u8, "Go back to Glamourer's Main Window."u8, buttonWidth))
            _mainWindow.IsOpen = true;

        _equipmentDrawer.Prepare(true);

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var data = EquipDrawData.FromState(_stateManager, _selection.State!, slot);
            _equipmentDrawer.DrawEquip(data);
        }

        var mainhand = EquipDrawData.FromState(_stateManager, _selection.State!, EquipSlot.MainHand);
        var offhand  = EquipDrawData.FromState(_stateManager, _selection.State!, EquipSlot.OffHand);
        _equipmentDrawer.DrawWeapons(mainhand, offhand, GameMain.IsInGPose());

        foreach (var slot in BonusExtensions.AllFlags)
        {
            var data = BonusDrawData.FromState(_stateManager, _selection.State!, slot);
            _equipmentDrawer.DrawBonusItem(data);
        }

        _equipmentDrawer.DrawDragDropTooltip();

        if (_selection.Data.Objects.Count > 0)
        {
            using var popupStyle = new Im.ColorStyleDisposable();
            popupStyle.PushDefault(ImStyleDouble.WindowPadding);
            popupStyle.PushDefault(ImStyleSingle.WindowBorderThickness);
            popupStyle.PushDefault(ImGuiColor.WindowBackground);
            popupStyle.PushDefault(ImGuiColor.Button);
            popupStyle.PushDefault(ImGuiColor.FrameBackground);

            _advancedDyes.Draw(_selection.Data.Objects.Last(), _selection.State!, true);
        }
    }
}
