using Glamourer.Unlocks;
using ImSharp;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class CustomizationUnlockPanel(CustomizeUnlockManager customizeUnlocks) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Customizations"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        using var table = Im.Table.Begin("customizationUnlocks"u8, 6,
            TableFlags.SizingFixedFit | TableFlags.RowBackground | TableFlags.ScrollY | TableFlags.BordersOuter,
            Im.ContentRegion.Available with { Y = 12 * Im.Style.TextHeight });
        if (!table)
            return;

        using var clipper = new Im.ListClipper(customizeUnlocks.Unlockable.Count, Im.Style.TextHeightWithSpacing);
        foreach (var (key, value) in clipper.Iterate(customizeUnlocks.Unlockable))
        {
            table.DrawColumn(key.Index.ToNameU8());
            table.DrawColumn($"{key.CustomizeId}");
            table.DrawColumn($"{key.Value.Value}");
            table.DrawColumn($"{value.Data}");
            table.DrawColumn(value.Name);
            table.DrawColumn(customizeUnlocks.IsUnlocked(key, out var time)
                ? time == DateTimeOffset.MinValue
                    ? "Always"u8
                    : $"{time.LocalDateTime:g}"
                : "Never"u8);
        }
    }
}
