using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Services;

public sealed class TextureService : TextureCache, IDisposable
{
    public TextureService(UiBuilder uiBuilder, IDataManager dataManager, ITextureProvider textureProvider)
        : base(dataManager, textureProvider)
        => _slotIcons = CreateSlotIcons(uiBuilder);

    private readonly IDalamudTextureWrap?[] _slotIcons;

    public (nint, Vector2, bool) GetIcon(EquipItem item, EquipSlot slot)
    {
        if (item.IconId.Id != 0 && TryLoadIcon(item.IconId.Id, out var ret))
            return (ret.ImGuiHandle, new Vector2(ret.Width, ret.Height), false);

        var idx = slot.ToIndex();
        return idx < 12 && _slotIcons[idx] != null
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

    private static IDalamudTextureWrap?[] CreateSlotIcons(UiBuilder uiBuilder)
    {
        var ret = new IDalamudTextureWrap?[12];

        using var uldWrapper = uiBuilder.LoadUld("ui/uld/ArmouryBoard.uld");

        if (!uldWrapper.Valid)
        {
            Glamourer.Log.Error($"Could not get empty slot uld.");
            return ret;
        }

        SetIcon(EquipSlot.Head,     1);
        SetIcon(EquipSlot.Body,     2);
        SetIcon(EquipSlot.Hands,    3);
        SetIcon(EquipSlot.Legs,     5);
        SetIcon(EquipSlot.Feet,     6);
        SetIcon(EquipSlot.Ears,     8);
        SetIcon(EquipSlot.Neck,     9);
        SetIcon(EquipSlot.Wrists,   10);
        SetIcon(EquipSlot.RFinger,  11);
        SetIcon(EquipSlot.MainHand, 0);
        SetIcon(EquipSlot.OffHand,  7);
        ret[EquipSlot.LFinger.ToIndex()] = ret[EquipSlot.RFinger.ToIndex()];

        return ret;

        void SetIcon(EquipSlot slot, int index)
        {
            try
            {
                ret[slot.ToIndex()] = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", index)!;
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not get empty slot texture for {slot.ToName()}, icon will be left empty. "
                  + $"This may be because of incompatible mods affecting your character screen interface:\n{ex}");
                ret[slot.ToIndex()] = null;
            }
        }
    }
}
