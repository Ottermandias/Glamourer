using System;
using System.Runtime.InteropServices;
using Glamourer.Customization;
using Glamourer.Interop;
using ImGuiScene;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Structs;
using Penumbra.String.Functions;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

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



[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CharacterData
{
    public const byte CurrentVersion = 3;

    public uint             ModelId;
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
        OffHand = designable.OffHand;
    }
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
