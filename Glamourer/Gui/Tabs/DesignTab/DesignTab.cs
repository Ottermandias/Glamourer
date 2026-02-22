using Dalamud.Interface.ImGuiNotification;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Interop;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignTab : TwoPanelLayout, ITab<MainTabType>
{
    private readonly ImportService _importService;
    private readonly DesignManager _manager;
    private readonly UiConfig      _uiConfig;

    public DesignTab(DesignFileSystemDrawer drawer, DesignPanel panel, ImportService importService, DesignManager manager, DesignFilter filter,
        DesignHeader header, UiConfig uiConfig)
    {
        LeftHeader = drawer.Header;
        LeftPanel  = drawer;
        LeftFooter = drawer.Footer;

        RightHeader    = header;
        RightPanel     = panel;
        RightFooter    = NopHeaderFooter.Instance;
        _importService = importService;
        _manager       = manager;
        _uiConfig      = uiConfig;
    }

    public override ReadOnlySpan<byte> Label
        => "Designs"u8;

    public MainTabType Identifier
        => MainTabType.Designs;

    protected override void DrawLeftGroup(in TwoPanelWidth width)
    {
        base.DrawLeftGroup(in width);
        if (_importService.CreateCharaTarget(out var designBase, out var name))
        {
            var newDesign = _manager.CreateClone(designBase, name, true);
            Glamourer.Messager.NotificationMessage($"Imported Anamnesis .chara file {name} as new design {newDesign.Name}",
                NotificationType.Success, false);
        }
        _importService.CreateCharaSource();
    }

    protected override float MinimumWidth
        => LeftFooter.MinimumWidth;

    protected override float MaximumWidth
        => Im.Window.Width - 500 * Im.Style.GlobalScale;

    protected override void SetWidth(float width, ScalingMode mode)
        => _uiConfig.DesignsTabScale = new TwoPanelWidth(width, mode);


    public void DrawContent()
        => Draw(_uiConfig.DesignsTabScale);
}
