using System;
using System.Numerics;
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

    private static IDalamudTextureWrap[] CreateSlotIcons(UiBuilder uiBuilder)
    {
        var ret = new IDalamudTextureWrap[12];

        using var uldWrapper = uiBuilder.LoadUld("ui/uld/ArmouryBoard.uld");

        if (!uldWrapper.Valid)
        {
            Glamourer.Log.Error($"Could not get empty slot uld.");
            return ret;
        }

        ret[0]  = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 1)!;
        ret[1]  = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 2)!;
        ret[2]  = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 3)!;
        ret[3]  = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 5)!;
        ret[4]  = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 6)!;
        ret[5]  = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 8)!;
        ret[6]  = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 9)!;
        ret[7]  = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 10)!;
        ret[8]  = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 11)!;
        ret[9]  = ret[8];
        ret[10] = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 0)!;
        ret[11] = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 7)!;

        uldWrapper.Dispose();
        return ret;
    }
}
