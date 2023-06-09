using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe class ChangeCustomizeService
{
    public ChangeCustomizeService()
        => SignatureHelper.Initialise(this);

    public delegate bool ChangeCustomizeDelegate(Human* human, byte* data, byte skipEquipment);

    [Signature(Sigs.ChangeCustomize)]
    private readonly ChangeCustomizeDelegate _changeCustomize = null!;

    public bool UpdateCustomize(Actor actor, CustomizeData customize)
    {
        if (customize.Data == null || !actor.Valid || !actor.DrawObject.Valid)
            return false;

        return _changeCustomize(actor.DrawObject.Pointer, customize.Data, 1);
    }
}
