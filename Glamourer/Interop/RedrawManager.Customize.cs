using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;

namespace Glamourer.Interop;

public unsafe partial class RedrawManager
{


    public static void SetVisor(Human* data, bool on)
    {
        if (data == null)
            return;

        var flags = &data->CharacterBase.UnkFlags_01;
        var state = (*flags & Offsets.DrawObjectVisorStateFlag) != 0;
        if (state == on)
            return;

        var newFlag = (byte)(on ? *flags | Offsets.DrawObjectVisorStateFlag : *flags & ~Offsets.DrawObjectVisorStateFlag);
        *flags = (byte) (newFlag | Offsets.DrawObjectVisorToggleFlag);
    }
}
