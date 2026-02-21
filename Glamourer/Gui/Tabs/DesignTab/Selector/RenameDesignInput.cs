using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class RenameDesignInput(DesignFileSystemDrawer fileSystem) : BaseButton<IFileSystemData>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemData _)
        => "##Rename"u8;

    /// <summary> Replaces the normal menu item handling for a text input, so the other fields are not used. </summary>
    /// <inheritdoc/>
    public override bool DrawMenuItem(in IFileSystemData data)
    {
        var       design      = (Design)data.Value;
        var       currentName = design.Name.Text;
        using var style       = Im.Style.PushDefault(ImStyleDouble.FramePadding);
        MenuSeparator.DrawSeparator();
        Im.Text("Rename Design:"u8);
        if (Im.Window.Appearing)
            Im.Keyboard.SetFocusHere();
        var ret = Im.Input.Text(Label(data), ref currentName, flags: InputTextFlags.EnterReturnsTrue);
        Im.Tooltip.OnHover("Enter a new name here to rename the changed design."u8);
        if (!ret)
            return false;

        fileSystem.Manager.Rename(design, currentName);
        Im.Popup.CloseCurrent();

        return ret;
    }
}
