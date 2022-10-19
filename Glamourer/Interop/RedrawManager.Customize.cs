using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;

namespace Glamourer.Interop;

public unsafe partial class RedrawManager
{
    // Update 
    public delegate bool ChangeCustomizeDelegate(Human* human, byte* data, byte skipEquipment);

    [Signature("E8 ?? ?? ?? ?? 41 0F B6 C5 66 41 89 86")]
    private readonly ChangeCustomizeDelegate _changeCustomize = null!;

    public bool UpdateCustomize(Actor actor, Customize customize)
    {
        if (!actor.Valid || !actor.DrawObject.Valid)
            return false;

        var d = actor.DrawObject;
        if (NeedsRedraw(d.Customize, customize))
        {
            Glamourer.Penumbra.RedrawObject(actor.Character, RedrawType.Redraw);
            return true;
        }

        return _changeCustomize(d.Pointer, (byte*)customize.Data, 1);
    }

    public static bool NeedsRedraw(Customize lhs, Customize rhs)
        => lhs.Race != rhs.Race
         || lhs.Gender != rhs.Gender
         || lhs.BodyType != rhs.BodyType
         || lhs.Face != rhs.Face
         || lhs.Race == Race.Hyur && lhs.Clan != rhs.Clan;


    public static void SetVisor(Human* data, bool on)
    {
        if (data == null)
            return;

        var flags = &data->CharacterBase.UnkFlags_01;
        var state = (*flags & 0x40) != 0;
        if (state == on)
            return;

        *flags = (byte)((on ? *flags | 0x40 : *flags & 0xBF) | 0x80);
    }
}
