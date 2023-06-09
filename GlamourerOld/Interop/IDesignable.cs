using Glamourer.Customization;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public interface IDesignable
{
    public bool            Valid         { get; }
    public uint            ModelId       { get; }
    public Customize       Customize     { get; }
    public CharacterEquip  Equip         { get; }
    public CharacterWeapon MainHand      { get; }
    public CharacterWeapon OffHand       { get; }
    public bool            VisorEnabled  { get; }
    public bool            WeaponEnabled { get; }
    public bool            IsWet         { get; }
}
