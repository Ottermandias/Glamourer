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
    public override void WriteJson(JsonWriter writer, CharacterSave? value, JsonSerializer serializer)
    {
        var s = value?.ToBase64() ?? string.Empty;
        serializer.Serialize(writer, s);
    }

    public override CharacterSave ReadJson(JsonReader reader, Type objectType, CharacterSave? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        var s     = token.ToObject<string>();
        return CharacterSave.FromString(s!);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct CharacterData
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
    public const byte TotalSizeVersion3 = 1 + 1 + 2 + 7 + 7 + 2 + 40 + CustomizationData.CustomizationBytes + 4;
    public const byte CurrentVersion    = 3;

    public  byte               Version;
    public  SaveFlags          Flags;
    public  CharacterEquipMask Equip;
    public  CharacterWeapon    MainHand;
    public  CharacterWeapon    OffHand;
    public  ushort             Padding;
    public  CharacterArmor     Head;
    public  CharacterArmor     Body;
    public  CharacterArmor     Hands;
    public  CharacterArmor     Legs;
    public  CharacterArmor     Feet;
    public  CharacterArmor     Ears;
    public  CharacterArmor     Neck;
    public  CharacterArmor     Wrist;
    public  CharacterArmor     RFinger;
    public  CharacterArmor     LFinger;
    private CustomizationData  CustomizationData;
    public  float              Alpha;

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

    public static readonly CharacterData Default
        = new()
        {
            Version           = CurrentVersion,
            Flags             = SaveFlags.WriteCustomizations,
            Equip             = CharacterEquipMask.All,
            MainHand          = CharacterWeapon.Empty,
            OffHand           = CharacterWeapon.Empty,
            Padding           = 0,
            Head              = CharacterArmor.Empty,
            Body              = CharacterArmor.Empty,
            Hands             = CharacterArmor.Empty,
            Legs              = CharacterArmor.Empty,
            Feet              = CharacterArmor.Empty,
            Ears              = CharacterArmor.Empty,
            Neck              = CharacterArmor.Empty,
            Wrist             = CharacterArmor.Empty,
            RFinger           = CharacterArmor.Empty,
            LFinger           = CharacterArmor.Empty,
            CustomizationData = CustomizationData.Default,
            Alpha             = 1f,
        };

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

    public string ToBase64()
    {
        fixed (void* ptr = &this)
        {
            return Convert.ToBase64String(new ReadOnlySpan<byte>(ptr, sizeof(CharacterData)));
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

    public static CharacterData FromString(string data)
    {
        var bytes = Convert.FromBase64String(data);
        var ret   = new CharacterData();
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
                    CheckSize(bytes.Length, TotalSizeVersion3);
                    Functions.MemCpyUnchecked(&ret, ptr, TotalSizeVersion3);
                    break;
                default: throw new Exception($"Can not parse Base64 string into CharacterSave:\n\tInvalid Version {bytes[0]}.");
            }
        }

        return ret;
    }
}

[JsonConverter(typeof(CharacterSaveConverter))]
public class CharacterSave
{
    private CharacterData _data;

    public CharacterSave()
        => _data = CharacterData.Default;

    public CharacterSave(Actor actor)
        => _data.Load(actor);

    public void Load(Actor actor)
        => _data.Load(actor);

    public string ToBase64()
        => _data.ToBase64();

    public CharacterCustomization Customization
        => _data.Customize;

    public CharacterEquip Equipment
        => _data.Equipment;

    public ref CharacterWeapon MainHand
        => ref _data.MainHand;

    public ref CharacterWeapon OffHand
        => ref _data.OffHand;

    public static CharacterSave FromString(string data)
        => new() { _data = CharacterData.FromString(data) };
}
