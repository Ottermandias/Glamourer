using Glamourer.GameData;
using Glamourer.Services;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class CustomizationServicePanel(CustomizeService customize) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Customization Service"u8;

    public bool Disabled
        => !customize.Finished;

    public void Draw()
    {
        foreach (var (clan, gender) in CustomizeManager.AllSets())
        {
            var set = customize.Manager.GetSet(clan, gender);
            DrawCustomizationInfo(set);
            DrawNpcCustomizationInfo(set);
        }

        DrawFacepaintInfo();
        DrawColorInfo();
    }


    private void DrawFacepaintInfo()
    {
        using var tree = Im.Tree.Node("NPC Facepaints"u8);
        if (!tree)
            return;

        using var table = Im.Table.Begin("data"u8, 2, TableFlags.SizingFixedFit | TableFlags.RowBackground);
        if (!table)
            return;

        table.NextColumn();
        table.Header("Id"u8);
        table.NextColumn();
        table.Header("Facepaint"u8);

        for (var i = 0; i < 128; ++i)
        {
            var index = new CustomizeValue((byte)i);
            table.DrawColumn($"{i:D3}");
            table.NextColumn();
            ImEx.Icon.Draw(customize.NpcCustomizeSet.CheckValue(CustomizeIndex.FacePaint, index) ? LunaStyle.TrueIcon : LunaStyle.FalseIcon);
        }
    }

    private void DrawColorInfo()
    {
        using var tree = Im.Tree.Node("NPC Colors"u8);
        if (!tree)
            return;

        using var table = Im.Table.Begin("data"u8, 5, TableFlags.SizingFixedFit | TableFlags.RowBackground);
        if (!table)
            return;

        table.NextColumn();
        table.Header("Id"u8);
        table.NextColumn();
        table.Header("Hair"u8);
        table.NextColumn();
        table.Header("Eyes"u8);
        table.NextColumn();
        table.Header("Facepaint"u8);
        table.NextColumn();
        table.Header("Tattoos"u8);

        for (var i = 192; i < 256; ++i)
        {
            var index = new CustomizeValue((byte)i);
            table.DrawColumn($"{i:D3}");
            table.NextColumn();
            ImEx.Icon.Draw(customize.NpcCustomizeSet.CheckValue(CustomizeIndex.HairColor, index) ? LunaStyle.TrueIcon : LunaStyle.FalseIcon);
            table.NextColumn();
            ImEx.Icon.Draw(customize.NpcCustomizeSet.CheckValue(CustomizeIndex.EyeColorLeft, index) ? LunaStyle.TrueIcon : LunaStyle.FalseIcon);
            table.NextColumn();
            ImEx.Icon.Draw(
                customize.NpcCustomizeSet.CheckValue(CustomizeIndex.FacePaintColor, index) ? LunaStyle.TrueIcon : LunaStyle.FalseIcon);
            table.NextColumn();
            ImEx.Icon.Draw(customize.NpcCustomizeSet.CheckValue(CustomizeIndex.TattooColor, index) ? LunaStyle.TrueIcon : LunaStyle.FalseIcon);
        }
    }

    private void DrawCustomizationInfo(CustomizeSet set)
    {
        using var tree = Im.Tree.Node($"{customize.ClanName(set.Clan, set.Gender)} {set.Gender}");
        if (!tree)
            return;

        using var table = Im.Table.Begin("data"u8, 5, TableFlags.SizingFixedFit | TableFlags.RowBackground);
        if (!table)
            return;

        foreach (var index in CustomizeIndex.Values)
        {
            table.DrawColumn(index.ToNameU8());
            table.DrawColumn(set.Option(index));
            table.DrawColumn(set.IsAvailable(index) ? "Available"u8 : "Unavailable"u8);
            table.DrawColumn(set.Type(index).ToNameU8());
            table.DrawColumn($"{set.Count(index)}");
        }
    }

    private void DrawNpcCustomizationInfo(CustomizeSet set)
    {
        using var tree = Im.Tree.Node($"{customize.ClanName(set.Clan, set.Gender)} {set.Gender} (NPC Options)");
        if (!tree)
            return;

        using var table = Im.Table.Begin("npc"u8, 2, TableFlags.SizingFixedFit | TableFlags.RowBackground);
        if (!table)
            return;

        foreach (var (index, value) in set.NpcOptions)
        {
            table.DrawColumn($"{index}");
            table.DrawColumn($"{value.Value}");
        }
    }
}
