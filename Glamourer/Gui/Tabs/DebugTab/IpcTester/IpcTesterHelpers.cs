using Glamourer.Api.Enums;
using ImSharp;

namespace Glamourer.Gui.Tabs.DebugTab.IpcTester;

public static class IpcTesterHelpers
{
    public static void DrawFlagInput(ref ApplyFlag flags)
    {
        var value = (flags & ApplyFlag.Once) is not 0;
        if (Im.Checkbox("Apply Once"u8, ref value))
            flags = value ? flags | ApplyFlag.Once : flags & ~ApplyFlag.Once;

        Im.Line.Same();
        value = (flags & ApplyFlag.Equipment) is not 0;
        if (Im.Checkbox("Apply Equipment"u8, ref value))
            flags = value ? flags | ApplyFlag.Equipment : flags & ~ApplyFlag.Equipment;

        Im.Line.Same();
        value = (flags & ApplyFlag.Customization) is not 0;
        if (Im.Checkbox("Apply Customization"u8, ref value))
            flags = value ? flags | ApplyFlag.Customization : flags & ~ApplyFlag.Customization;

        Im.Line.Same();
        value = (flags & ApplyFlag.Lock) is not 0;
        if (Im.Checkbox("Lock Actor"u8, ref value))
            flags = value ? flags | ApplyFlag.Lock : flags & ~ApplyFlag.Lock;
    }

    public static void IndexInput(ref int index)
    {
        Im.Item.SetNextWidth(Im.ContentRegion.Available.X / 2);
        Im.Input.Scalar("Game Object Index"u8, ref index);
    }

    public static void KeyInput(ref uint key)
    {
        Im.Item.SetNextWidth(Im.ContentRegion.Available.X / 2);
        var keyI = (int)key;
        if (Im.Input.Scalar("Key"u8, ref keyI))
            key = (uint)keyI;
    }

    public static void NameInput(ref string name)
    {
        Im.Item.SetNextWidthFull();
        Im.Input.Text("##gameObject"u8, ref name, "Character Name..."u8);
    }

    public static void DrawIntro(ReadOnlySpan<byte> intro)
    {
        Im.Table.NextColumn();
        ImEx.TextFrameAligned(intro);
        Im.Table.NextColumn();
    }
}
