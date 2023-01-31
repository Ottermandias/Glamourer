using System.Runtime.InteropServices;
using Glamourer.Customization;
using Glamourer.Interop;
using Penumbra.GameData.Structs;
using Penumbra.String.Functions;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Designs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CharacterData
{
    public uint            ModelId;
    public CustomizeData   CustomizeData;
    public CharacterWeapon MainHand;
    public CharacterWeapon OffHand;
    public CharacterArmor  Head;
    public CharacterArmor  Body;
    public CharacterArmor  Hands;
    public CharacterArmor  Legs;
    public CharacterArmor  Feet;
    public CharacterArmor  Ears;
    public CharacterArmor  Neck;
    public CharacterArmor  Wrists;
    public CharacterArmor  RFinger;
    public CharacterArmor  LFinger;

    public unsafe Customize Customize
    {
        get
        {
            fixed (CustomizeData* ptr = &CustomizeData)
            {
                return new Customize(ptr);
            }
        }
    }

    public unsafe CharacterEquip Equipment
    {
        get
        {
            fixed (CharacterArmor* ptr = &Head)
            {
                return new CharacterEquip(ptr);
            }
        }
    }

    public static readonly CharacterData Default
        = new()
        {
            ModelId       = 0,
            CustomizeData = Customize.Default,
            MainHand      = CharacterWeapon.Empty,
            OffHand       = CharacterWeapon.Empty,
            Head          = CharacterArmor.Empty,
            Body          = CharacterArmor.Empty,
            Hands         = CharacterArmor.Empty,
            Legs          = CharacterArmor.Empty,
            Feet          = CharacterArmor.Empty,
            Ears          = CharacterArmor.Empty,
            Neck          = CharacterArmor.Empty,
            Wrists        = CharacterArmor.Empty,
            RFinger       = CharacterArmor.Empty,
            LFinger       = CharacterArmor.Empty,
        };

    public readonly unsafe CharacterData Clone()
    {
        var data = new CharacterData();
        fixed (void* ptr = &this)
        {
            MemoryUtility.MemCpyUnchecked(&data, ptr, sizeof(CharacterData));
        }

        return data;
    }

    public void Load(IDesignable designable)
    {
        ModelId = designable.ModelId;
        Customize.Load(designable.Customize);
        Equipment.Load(designable.Equip);
        MainHand = designable.MainHand;
        OffHand  = designable.OffHand;
    }
}
