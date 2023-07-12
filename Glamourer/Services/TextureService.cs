using System;
using System.Linq;
using Dalamud.Data;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Interface;
using ImGuiScene;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Services;

public sealed class TextureService : TextureCache, IDisposable
{
    public TextureService(Framework framework, UiBuilder uiBuilder, DataManager dataManager)
        : base(framework, uiBuilder, dataManager)
        => _slotIcons = CreateSlotIcons(uiBuilder);

    private readonly TextureWrap?[] _slotIcons;

    public (nint, Vector2, bool) GetIcon(EquipItem item)
    {
        if (item.IconId != 0 && TryLoadIcon(item.IconId, out var ret))
            return (ret.Value.Texture, ret.Value.Dimensions, false);

        var idx = item.Type.ToSlot().ToIndex();
        return idx < 12 && _slotIcons[idx] != null
            ? (_slotIcons[idx]!.ImGuiHandle, new Vector2(_slotIcons[idx]!.Width, _slotIcons[idx]!.Height), true)
            : (nint.Zero, Vector2.Zero, true);
    }

    public new void Dispose()
    {
        base.Dispose();
        for (var i = 0; i < _slotIcons.Length; ++i)
        {
            _slotIcons[i]?.Dispose();
            _slotIcons[i] = null;
        }
    }

    private static TextureWrap[] CreateSlotIcons(UiBuilder uiBuilder)
    {
        var ret = new TextureWrap[12];

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
        ret[9]  = ret[10];
        ret[10] = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 0)!;
        ret[11] = uldWrapper.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 7)!;

        uldWrapper.Dispose();
        return ret;
    }
}
