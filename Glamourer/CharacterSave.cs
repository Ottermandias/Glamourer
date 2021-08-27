using System;
using Dalamud.Game.ClientState.Objects.Types;
using Glamourer.Customization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Structs;

namespace Glamourer
{
    public class CharacterSaveConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(CharacterSave);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var s     = token.ToObject<string>();
            return CharacterSave.FromString(s!);
        }

        public override bool CanWrite
            => true;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value != null)
            {
                var s = ((CharacterSave) value).ToBase64();
                serializer.Serialize(writer, s);
            }
        }
    }

    [JsonConverter(typeof(CharacterSaveConverter))]
    public class CharacterSave
    {
        public const byte CurrentVersion    = 2;
        public const byte TotalSizeVersion1 = 1 + 1 + 2 + 56 + ActorCustomization.CustomizationBytes;
        public const byte TotalSizeVersion2 = 1 + 1 + 2 + 56 + ActorCustomization.CustomizationBytes + 4 + 1;

        public const byte TotalSize = TotalSizeVersion2;

        private readonly byte[] _bytes = new byte[TotalSize];

        public CharacterSave()
        {
            _bytes[0] = CurrentVersion;
            Alpha     = 1.0f;
        }

        public CharacterSave Copy()
        {
            var ret = new CharacterSave();
            _bytes.CopyTo((Span<byte>) ret._bytes);
            return ret;
        }

        public byte Version
            => _bytes[0];

        public bool WriteCustomizations
        {
            get => (_bytes[1] & 0x01) != 0;
            set => _bytes[1] = (byte) (value ? _bytes[1] | 0x01 : _bytes[1] & ~0x01);
        }

        public bool IsWet
        {
            get => (_bytes[1] & 0x02) != 0;
            set => _bytes[1] = (byte) (value ? _bytes[1] | 0x02 : _bytes[1] & ~0x02);
        }

        public bool SetHatState
        {
            get => (_bytes[1] & 0x04) != 0;
            set => _bytes[1] = (byte) (value ? _bytes[1] | 0x04 : _bytes[1] & ~0x04);
        }

        public bool SetWeaponState
        {
            get => (_bytes[1] & 0x08) != 0;
            set => _bytes[1] = (byte) (value ? _bytes[1] | 0x08 : _bytes[1] & ~0x08);
        }

        public bool SetVisorState
        {
            get => (_bytes[1] & 0x10) != 0;
            set => _bytes[1] = (byte) (value ? _bytes[1] | 0x10 : _bytes[1] & ~0x10);
        }

        public bool WriteProtected
        {
            get => (_bytes[1] & 0x20) != 0;
            set => _bytes[1] = (byte) (value ? _bytes[1] | 0x20 : _bytes[1] & ~0x20);
        }

        public byte StateFlags
        {
            get => _bytes[64 + ActorCustomization.CustomizationBytes];
            set => _bytes[64 + ActorCustomization.CustomizationBytes] = value;
        }

        public bool HatState
        {
            get => (StateFlags & 0x01) == 0;
            set => StateFlags = (byte) (value ? StateFlags & ~0x01 : StateFlags | 0x01);
        }

        public bool VisorState
        {
            get => (StateFlags & 0x10) != 0;
            set => StateFlags = (byte) (value ? StateFlags | 0x10 : StateFlags & ~0x10);
        }

        public bool WeaponState
        {
            get => (StateFlags & 0x02) == 0;
            set => StateFlags = (byte) (value ? StateFlags & ~0x02 : StateFlags | 0x02);
        }

        public ActorEquipMask WriteEquipment
        {
            get => (ActorEquipMask) (_bytes[2] | (_bytes[3] << 8));
            set
            {
                _bytes[2] = (byte) ((ushort) value & 0xFF);
                _bytes[3] = (byte) ((ushort) value >> 8);
            }
        }

        public unsafe float Alpha
        {
            get
            {
                fixed (byte* ptr = &_bytes[60 + ActorCustomization.CustomizationBytes])
                {
                    return *(float*) ptr;
                }
            }
            set
            {
                fixed (byte* ptr = _bytes)
                {
                    *(ptr + 60 + ActorCustomization.CustomizationBytes + 0) = *((byte*) &value + 0);
                    *(ptr + 60 + ActorCustomization.CustomizationBytes + 1) = *((byte*) &value + 1);
                    *(ptr + 60 + ActorCustomization.CustomizationBytes + 2) = *((byte*) &value + 2);
                    *(ptr + 60 + ActorCustomization.CustomizationBytes + 3) = *((byte*) &value + 3);
                }
            }
        }

        public void Load(ActorCustomization customization)
        {
            WriteCustomizations = true;
            customization.WriteBytes(_bytes, 4);
        }

        public void Load(ActorEquipment equipment, ActorEquipMask mask = ActorEquipMask.All)
        {
            WriteEquipment = mask;
            equipment.WriteBytes(_bytes, 4 + ActorCustomization.CustomizationBytes);
        }

        public string ToBase64()
            => Convert.ToBase64String(_bytes);

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

        private static void CheckActorMask(byte val1, byte val2)
        {
            var mask = (ActorEquipMask) (val1 | (val2 << 8));
            if (mask > ActorEquipMask.All)
                throw new Exception($"Can not parse Base64 string into CharacterSave:\n\tInvalid value {mask} in byte 3 and 4.");
        }

        public void LoadActor(Character a)
        {
            WriteCustomizations = true;
            Load(new ActorCustomization(a));

            Load(new ActorEquipment(a));

            SetHatState    = true;
            SetVisorState  = true;
            SetWeaponState = true;
            StateFlags     = a.StateFlags();

            IsWet = a.IsWet();
            Alpha = a.Alpha();
        }

        public void Apply(Character a)
        {
            if (WriteCustomizations)
                Customizations.Write(a.Address);
            if (WriteEquipment != ActorEquipMask.None)
                Equipment.Write(a.Address, WriteEquipment, WriteEquipment);
            a.SetWetness(IsWet);
            a.Alpha() = Alpha;
            if ((_bytes[1] & 0b11100) == 0b11100)
            {
                a.StateFlags() = StateFlags;
            }
            else
            {
                if (SetHatState)
                    a.SetHatHidden(HatState);
                if (SetVisorState)
                    a.SetVisorToggled(VisorState);
                if (SetWeaponState)
                    a.SetWeaponHidden(WeaponState);
            }
        }

        public void Load(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            switch (bytes[0])
            {
                case 1:
                    CheckSize(bytes.Length, TotalSizeVersion1);
                    CheckRange(2, bytes[1], 0, 1);
                    Alpha    = 1.0f;
                    bytes[0] = CurrentVersion;
                    break;
                case 2:
                    CheckSize(bytes.Length, TotalSizeVersion2);
                    CheckRange(2, bytes[1], 0, 0x3F);
                    break;
                default: throw new Exception($"Can not parse Base64 string into CharacterSave:\n\tInvalid Version {bytes[0]}.");
            }

            CheckActorMask(bytes[2], bytes[3]);
            bytes.CopyTo(_bytes, 0);
        }

        public static CharacterSave FromString(string base64)
        {
            var ret = new CharacterSave();
            ret.Load(base64);
            return ret;
        }

        public unsafe ref ActorCustomization Customizations
        {
            get
            {
                fixed (byte* ptr = _bytes)
                {
                    return ref *(ActorCustomization*) (ptr + 4);
                }
            }
        }

        public ActorEquipment Equipment
        {
            get
            {
                var ret = new ActorEquipment();
                ret.FromBytes(_bytes, 4 + ActorCustomization.CustomizationBytes);
                return ret;
            }
        }
    }
}
