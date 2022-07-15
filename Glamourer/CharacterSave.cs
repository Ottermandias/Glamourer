using System;
using System.Linq;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Glamourer.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Structs;
using Functions = Penumbra.GameData.Util.Functions;

namespace Glamourer;

public class CharacterSaveConverter : JsonConverter<CharacterSave>
{
    public override void WriteJson(JsonWriter writer, CharacterSave value, JsonSerializer serializer)
    {
        var s = value.ToBase64();
        serializer.Serialize(writer, s);
    }

    public override CharacterSave ReadJson(JsonReader reader, Type objectType, CharacterSave existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        var s     = token.ToObject<string>();
        return CharacterSave.FromString(s!);
    }
}

[JsonConverter(typeof(CharacterSaveConverter))]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct CharacterSave
{
    [Flags]
    public enum SaveFlags : byte
    {
        WriteCustomizations = 0x01,
        IsWet               = 0x02,
        SetHatState         = 0x04,
        SetWeaponState      = 0x08,
        SetVisorState       = 0x10,
        HatState            = 0x20,
        WeaponState         = 0x40,
        VisorState          = 0x80,
    }

    public const byte TotalSizeVersion1 = 1 + 1 + 2 + 56 + CustomizationData.CustomizationBytes;
    public const byte TotalSizeVersion2 = 1 + 1 + 2 + 56 + CustomizationData.CustomizationBytes + 4 + 1;

    public const byte               CurrentVersion    = 3;
    public       byte               Version           = CurrentVersion;
    public       SaveFlags          Flags             = 0;
    public       CharacterEquipMask Equip             = 0;
    public       CharacterWeapon    MainHand          = default;
    public       CharacterWeapon    OffHand           = default;
    public       ushort             Padding           = 0;
    public       CharacterArmor     Head              = default;
    public       CharacterArmor     Body              = default;
    public       CharacterArmor     Hands             = default;
    public       CharacterArmor     Legs              = default;
    public       CharacterArmor     Feet              = default;
    public       CharacterArmor     Ears              = default;
    public       CharacterArmor     Neck              = default;
    public       CharacterArmor     Wrist             = default;
    public       CharacterArmor     RFinger           = default;
    public       CharacterArmor     LFinger           = default;
    private      CustomizationData  CustomizationData = CustomizationData.Default;
    public       float              Alpha             = 1f;

    public CharacterSave()
    { }

    public void Load(Actor actor)
    {
        if (!actor.IsHuman || actor.Pointer->GameObject.DrawObject == null)
            return;

        var human = (Human*)actor.Pointer->GameObject.DrawObject;
        CustomizationData = *(CustomizationData*)human->CustomizeData;
        fixed (void* equip = &Head)
        {
            Functions.MemCpyUnchecked(equip, human->EquipSlotData, sizeof(CharacterArmor) * 10);
        }
    }

    public CharacterCustomization Customize
    {
        get
        {
            fixed (CustomizationData* ptr = &CustomizationData)
            {
                return new CharacterCustomization(ptr);
            }
        }
    }

    public CharacterEquip Equipment
    {
        get
        {
            fixed (CharacterArmor* ptr = &Head)
            {
                return new CharacterEquip(ptr);
            }
        }
    }

    public string ToBase64()
    {
        fixed (void* ptr = &this)
        {
            return Convert.ToBase64String(new ReadOnlySpan<byte>(ptr, sizeof(CharacterSave)));
        }
    }

    private static void CheckSize(int length, int requiredLength)
    {
        if (length != requiredLength)
            throw new Exception(
                $"Can not parse Base64 string into CharacterSave:\n\tInvalid size {length} instead of {requiredLength}.");
    }

    private static void CheckRange(int idx, byte value, byte min, byte max)
    {
        if (value < min || value > max)
            throw new Exception(
                $"Can not parse Base64 string into CharacterSave:\n\tInvalid value {value} in byte {idx}, should be in [{min},{max}].");
    }

    public static CharacterSave FromString(string data)
    {
        var bytes = Convert.FromBase64String(data);
        var ret   = new CharacterSave();
        fixed (byte* ptr = bytes)
        {
            switch (bytes[0])
            {
                case 1:
                    CheckSize(bytes.Length, TotalSizeVersion1);
                    CheckRange(2, bytes[1], 0, 1);
                    Functions.MemCpyUnchecked(&ret, ptr, TotalSizeVersion1);
                    ret.Version = CurrentVersion;
                    ret.Alpha   = 1f;
                    break;
                case 2:
                    CheckSize(bytes.Length, TotalSizeVersion2);
                    CheckRange(2, bytes[1], 0, 0x3F);
                    Functions.MemCpyUnchecked(&ret, ptr, TotalSizeVersion2 - 1);
                    ret.Flags &= ~SaveFlags.HatState;
                    if ((bytes.Last() & 0x01) != 0)
                        ret.Flags |= SaveFlags.HatState;
                    if ((bytes.Last() & 0x02) != 0)
                        ret.Flags |= SaveFlags.WeaponState;
                    if ((bytes.Last() & 0x04) != 0)
                        ret.Flags |= SaveFlags.VisorState;
                    break;
                case 3:
                    CheckSize(bytes.Length, sizeof(CharacterSave));
                    Functions.MemCpyUnchecked(&ret, ptr, sizeof(CharacterSave));
                    break;
                default: throw new Exception($"Can not parse Base64 string into CharacterSave:\n\tInvalid Version {bytes[0]}.");
            }
        }

        return ret;
    }
}
