using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Services;

public sealed class TextureService(IUiBuilder uiBuilder, IDataManager dataManager, ITextureProvider textureProvider)
    : TextureCache(dataManager, textureProvider), IDisposable
{
    private readonly IDalamudTextureWrap?[] _slotIcons = CreateSlotIcons(uiBuilder);

    public (nint, Vector2, bool) GetIcon(EquipItem item, EquipSlot slot)
    {
        if (item.IconId.Id != 0 && TryLoadIcon(item.IconId.Id, out var ret))
            return (ret.ImGuiHandle, new Vector2(ret.Width, ret.Height), false);

        var idx = slot.ToIndex();
        return idx < 12 && _slotIcons[idx] != null
            ? (_slotIcons[idx]!.ImGuiHandle, new Vector2(_slotIcons[idx]!.Width, _slotIcons[idx]!.Height), true)
            : (nint.Zero, Vector2.Zero, true);
    }

    public (nint, Vector2, bool) GetIcon(EquipItem item, BonusItemFlag slot)
    {
        if (item.IconId.Id != 0 && TryLoadIcon(item.IconId.Id, out var ret))
            return (ret.ImGuiHandle, new Vector2(ret.Width, ret.Height), false);

        var idx = slot.ToIndex();
        if (idx == uint.MaxValue)
            return (nint.Zero, Vector2.Zero, true);

        idx += 12;
        return idx < 13 && _slotIcons[idx] != null
            ? (_slotIcons[idx]!.ImGuiHandle, new Vector2(_slotIcons[idx]!.Width, _slotIcons[idx]!.Height), true)
            : (nint.Zero, Vector2.Zero, true);
    }

    public void Dispose()
    {
        for (var i = 0; i < _slotIcons.Length; ++i)
        {
            _slotIcons[i]?.Dispose();
            _slotIcons[i] = null;
        }
    }

    private static IDalamudTextureWrap?[] CreateSlotIcons(IUiBuilder uiBuilder)
    {
        var ret = new IDalamudTextureWrap?[13];

        using var uldWrapper = uiBuilder.LoadUld("ui/uld/Character.uld");

        if (!uldWrapper.Valid)
        {
            Glamourer.Log.Error($"Could not get empty slot uld.");
            return ret;
        }

        SetIcon(EquipSlot.Head,     19);
        SetIcon(EquipSlot.Body,     20);
        SetIcon(EquipSlot.Hands,    21);
        SetIcon(EquipSlot.Legs,     23);
        SetIcon(EquipSlot.Feet,     24);
        SetIcon(EquipSlot.Ears,     25);
        SetIcon(EquipSlot.Neck,     26);
        SetIcon(EquipSlot.Wrists,   27);
        SetIcon(EquipSlot.RFinger,  28);
        SetIcon(EquipSlot.MainHand, 17);
        SetIcon(EquipSlot.OffHand,  18);
        Set(BonusItemFlag.Glasses.ToName(), (int) BonusItemFlag.Glasses.ToIndex() + 12, 55);
        ret[EquipSlot.LFinger.ToIndex()] = ret[EquipSlot.RFinger.ToIndex()];

        return ret;

        void Set(string name, int slot, int index)
        {
            try
            {
                ret[slot] = uldWrapper.LoadTexturePart("ui/uld/Character_hr1.tex", index)!;
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not get empty slot texture for {name}, icon will be left empty. "
                  + $"This may be because of incompatible mods affecting your character screen interface:\n{ex}");
                ret[slot] = null;
            }
        }

        void SetIcon(EquipSlot slot, int index)
            => Set(slot.ToName(), (int)slot.ToIndex(), index);
    }
}
