using System;
using System.Linq;
using Glamourer.Services;
using ImGuiNET;
using OtterGui.Raii;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.DebugTab;

public class ItemManagerPanel(ItemManager _items) : IDebugTabTree
{
    public string Label
        => "Item Manager";

    public bool Disabled
        => !_items.ItemService.Valid;

    private string _itemFilter = string.Empty;

    public void Draw()
    {
        ImRaii.TreeNode($"Default Sword: {_items.DefaultSword.Name} ({_items.DefaultSword.ItemId}) ({_items.DefaultSword.Weapon()})",
            ImGuiTreeNodeFlags.Leaf).Dispose();
        DebugTab.DrawNameTable("All Items (Main)", ref _itemFilter,
            _items.ItemService.AwaitedService.AllItems(true).Select(p => (p.Item1.Id,
                    $"{p.Item2.Name} ({(p.Item2.WeaponType == 0 ? p.Item2.Armor().ToString() : p.Item2.Weapon().ToString())})"))
                .OrderBy(p => p.Item1));
        DebugTab.DrawNameTable("All Items (Off)", ref _itemFilter,
            _items.ItemService.AwaitedService.AllItems(false).Select(p => (p.Item1.Id,
                    $"{p.Item2.Name} ({(p.Item2.WeaponType == 0 ? p.Item2.Armor().ToString() : p.Item2.Weapon().ToString())})"))
                .OrderBy(p => p.Item1));
        foreach (var type in Enum.GetValues<FullEquipType>().Skip(1))
        {
            DebugTab.DrawNameTable(type.ToName(), ref _itemFilter,
                _items.ItemService.AwaitedService[type]
                    .Select(p => (p.ItemId.Id, $"{p.Name} ({(p.WeaponType == 0 ? p.Armor().ToString() : p.Weapon().ToString())})")));
        }
    }
}
