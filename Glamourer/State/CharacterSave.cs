using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Glamourer.Interop;
using Glamourer.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using DrawObject = Glamourer.Interop.DrawObject;
using Functions = Penumbra.GameData.Util.Functions;

namespace Glamourer.State;

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

[Flags]
public enum ApplicationFlags : uint
{
    Customizations = 0x000001,
    MainHand       = 0x000002,
    OffHand        = 0x000004,
    Head           = 0x000008,
    Body           = 0x000010,
    Hands          = 0x000020,
    Legs           = 0x000040,
    Feet           = 0x000080,
    Ears           = 0x000100,
    Neck           = 0x000200,
    Wrist          = 0x000400,
    RFinger        = 0x000800,
    LFinger        = 0x001000,
    SetVisor       = 0x002000,
    Visor          = 0x004000,
    SetWeapon      = 0x008000,
    Weapon         = 0x010000,
    SetWet         = 0x020000,
    Wet            = 0x040000,
}

public static class ApplicationFlagExtensions
{
    public static ApplicationFlags ToApplicationFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand          => ApplicationFlags.MainHand,
            EquipSlot.OffHand           => ApplicationFlags.OffHand,
            EquipSlot.Head              => ApplicationFlags.Head,
            EquipSlot.Body              => ApplicationFlags.Body,
            EquipSlot.Hands             => ApplicationFlags.Hands,
            EquipSlot.Legs              => ApplicationFlags.Legs,
            EquipSlot.Feet              => ApplicationFlags.Feet,
            EquipSlot.Ears              => ApplicationFlags.Ears,
            EquipSlot.Neck              => ApplicationFlags.Neck,
            EquipSlot.Wrists            => ApplicationFlags.Wrist,
            EquipSlot.RFinger           => ApplicationFlags.RFinger,
            EquipSlot.BothHand          => ApplicationFlags.MainHand | ApplicationFlags.OffHand,
            EquipSlot.LFinger           => ApplicationFlags.LFinger,
            EquipSlot.HeadBody          => ApplicationFlags.Body,
            EquipSlot.BodyHandsLegsFeet => ApplicationFlags.Body,
            EquipSlot.LegsFeet          => ApplicationFlags.Legs,
            EquipSlot.FullBody          => ApplicationFlags.Body,
            EquipSlot.BodyHands         => ApplicationFlags.Body,
            EquipSlot.BodyLegsFeet      => ApplicationFlags.Body,
            EquipSlot.ChestHands        => ApplicationFlags.Body,
            _                           => 0,
        };

    public static EquipSlot ToSlot(this ApplicationFlags flags)
        => flags switch
        {
            ApplicationFlags.MainHand => EquipSlot.MainHand,
            ApplicationFlags.OffHand  => EquipSlot.OffHand,
            ApplicationFlags.Head     => EquipSlot.Head,
            ApplicationFlags.Body     => EquipSlot.Body,
            ApplicationFlags.Hands    => EquipSlot.Hands,
            ApplicationFlags.Legs     => EquipSlot.Legs,
            ApplicationFlags.Feet     => EquipSlot.Feet,
            ApplicationFlags.Ears     => EquipSlot.Ears,
            ApplicationFlags.Neck     => EquipSlot.Neck,
            ApplicationFlags.Wrist    => EquipSlot.Wrists,
            ApplicationFlags.RFinger  => EquipSlot.RFinger,
            ApplicationFlags.LFinger  => EquipSlot.LFinger,
            _                         => EquipSlot.Unknown,
        };
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CharacterData
{
    public const byte CurrentVersion = 3;

    public uint             ModelId;
    public ApplicationFlags Flags;
    public CustomizeData    CustomizeData;
    public CharacterWeapon  MainHand;
    public CharacterWeapon  OffHand;
    public CharacterArmor   Head;
    public CharacterArmor   Body;
    public CharacterArmor   Hands;
    public CharacterArmor   Legs;
    public CharacterArmor   Feet;
    public CharacterArmor   Ears;
    public CharacterArmor   Neck;
    public CharacterArmor   Wrist;
    public CharacterArmor   RFinger;
    public CharacterArmor   LFinger;

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
            Flags         = 0,
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
            Wrist         = CharacterArmor.Empty,
            RFinger       = CharacterArmor.Empty,
            LFinger       = CharacterArmor.Empty,
        };

    public unsafe CharacterData Clone()
    {
        var data = new CharacterData();
        fixed (void* ptr = &this)
        {
            Functions.MemCpyUnchecked(&data, ptr, sizeof(CharacterData));
        }

        return data;
    }

    private const ApplicationFlags SaveFlags = ApplicationFlags.Customizations
      | ApplicationFlags.Head
      | ApplicationFlags.Body
      | ApplicationFlags.Hands
      | ApplicationFlags.Legs
      | ApplicationFlags.Feet
      | ApplicationFlags.Ears
      | ApplicationFlags.Neck
      | ApplicationFlags.Wrist
      | ApplicationFlags.RFinger
      | ApplicationFlags.LFinger
      | ApplicationFlags.MainHand
      | ApplicationFlags.OffHand
      | ApplicationFlags.SetVisor
      | ApplicationFlags.SetWeapon;


    public void Load(IDesignable designable)
    {
        ModelId = designable.ModelId;
        Customize.Load(designable.Customize);
        Equipment.Load(designable.Equip);
        MainHand = designable.MainHand;
        OffHand = designable.OffHand;
        Flags = SaveFlags | (designable.VisorEnabled ? ApplicationFlags.Visor : 0) | (designable.WeaponEnabled ? ApplicationFlags.Weapon : 0);
    }
}

public interface ICharacterData
{
    public ref CharacterData Data { get; }

    //public        bool ApplyModel();
    //public        bool ApplyCustomize(Customize target);
    //public        bool ApplyWeapon(ref CharacterWeapon weapon, bool mainHand, bool offHand);
    //public        bool ApplyGear(ref CharacterArmor armor, EquipSlot slot);
    //public unsafe bool ApplyWetness(CharacterBase* drawObject);
    //public unsafe bool ApplyVisorState(CharacterBase* drawObject);
    //public unsafe bool ApplyWeaponState(CharacterBase* drawObject);
}

[JsonConverter(typeof(CharacterSaveConverter))]
public class CharacterSave
{
    private CharacterData _data = CharacterData.Default;

    public CharacterSave()
    { }

    public CharacterSave(Actor actor)
    {
        Load(actor);
    }

    public void Load<T>(T actor) where T : IDesignable
    {
        _data.Load(actor);
    }

    public string ToBase64()
        => string.Empty;

    public Customize Customize
        => _data.Customize;

    public CharacterEquip Equipment
        => _data.Equipment;

    public ref CharacterWeapon MainHand
        => ref _data.MainHand;

    public ref CharacterWeapon OffHand
        => ref _data.OffHand;

    public static CharacterSave FromString(string data)
        => new();
}
