using System.Numerics;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignPanel
{
    private readonly DesignFileSystemSelector _selector;
    private readonly DesignManager            _manager;
    private readonly CustomizationDrawer      _customizationDrawer;

    public DesignPanel(DesignFileSystemSelector selector, CustomizationDrawer customizationDrawer, DesignManager manager)
    {
        _selector            = selector;
        _customizationDrawer = customizationDrawer;
        _manager             = manager;
    }

    public void Draw()
    {
        var design = _selector.Selected;
        if (design == null)
            return;

        using var child = ImRaii.Child("##panel", -Vector2.One, true);
        if (!child)
            return;

        _customizationDrawer.Draw(design.DesignData.Customize, design.WriteProtected());
    }
}
