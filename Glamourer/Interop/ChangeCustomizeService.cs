using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Interop.Structs;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

/// <summary>
/// Access the function the game uses to update customize data on the character screen.
/// Changes in Race, body type or Gender are probably ignored.
/// This operates on draw objects, not game objects.
/// </summary>
public unsafe class ChangeCustomizeService
{
    public ChangeCustomizeService()
        => SignatureHelper.Initialise(this);

    private delegate bool ChangeCustomizeDelegate(Human* human, byte* data, byte skipEquipment);

    [Signature(Sigs.ChangeCustomize)]
    private readonly ChangeCustomizeDelegate _changeCustomize = null!;

    public bool UpdateCustomize(Model model, CustomizeData customize)
    {
        if (!model.IsHuman)
            return false;

        Item.Log.Verbose($"[ChangeCustomize] Invoked on 0x{model.Address:X} with {customize}.");
        return _changeCustomize(model.AsHuman, customize.Data, 1);
    }

    public bool UpdateCustomize(Actor actor, CustomizeData customize)
        => UpdateCustomize(actor.Model, customize);
}
