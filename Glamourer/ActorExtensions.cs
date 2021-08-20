using Dalamud.Game.ClientState.Actors.Types;

namespace Glamourer
{
    public static class ActorExtensions
    {
        public const int  WetnessOffset      = 0x19A5;
        public const byte WetnessFlag        = 0x10;
        public const int  StateFlagsOffset   = 0x106C;
        public const byte HatHiddenFlag      = 0x01;
        public const byte VisorToggledFlag   = 0x10;
        public const int  AlphaOffset        = 0x182C;
        public const int  WeaponHiddenOffset = 0xF64;
        public const byte WeaponHiddenFlag   = 0x02;

        public static unsafe bool IsWet(this Actor a)
            => (*((byte*) a.Address + WetnessOffset) & WetnessFlag) != 0;

        public static unsafe bool SetWetness(this Actor a, bool value)
        {
            var current = a.IsWet();
            if (current == value)
                return false;

            if (value)
                *((byte*) a.Address + WetnessOffset) = (byte) (*((byte*) a.Address + WetnessOffset) | WetnessFlag);
            else
                *((byte*) a.Address + WetnessOffset) = (byte) (*((byte*) a.Address + WetnessOffset) & ~WetnessFlag);
            return true;
        }

        public static unsafe ref byte StateFlags(this Actor a)
            => ref *((byte*) a.Address + StateFlagsOffset);

        public static bool SetStateFlag(this Actor a, bool value, byte flag)
        {
            var current       = a.StateFlags();
            var previousValue = (current & flag) != 0;
            if (previousValue == value)
                return false;

            if (value)
                a.StateFlags() = (byte) (current | flag);
            else
                a.StateFlags() = (byte) (current & ~flag);
            return true;
        }

        public static bool IsHatHidden(this Actor a)
            => (a.StateFlags() & HatHiddenFlag) != 0;

        public static unsafe bool IsWeaponHidden(this Actor a)
            => (a.StateFlags() & WeaponHiddenFlag) != 0
             && (*((byte*) a.Address + WeaponHiddenOffset) & WeaponHiddenFlag) != 0;

        public static bool IsVisorToggled(this Actor a)
            => (a.StateFlags() & VisorToggledFlag) != 0;

        public static bool SetHatHidden(this Actor a, bool value)
            => SetStateFlag(a, value, HatHiddenFlag);

        public static unsafe bool SetWeaponHidden(this Actor a, bool value)
        {
            var ret = SetStateFlag(a, value, WeaponHiddenFlag);
            var val = *((byte*) a.Address + WeaponHiddenOffset);
            if (value)
                *((byte*) a.Address + WeaponHiddenOffset) = (byte) (val | WeaponHiddenFlag);
            else
                *((byte*) a.Address + WeaponHiddenOffset) = (byte) (val & ~WeaponHiddenFlag);
            return ret || ((val & WeaponHiddenFlag) != 0) != value;
        }

        public static bool SetVisorToggled(this Actor a, bool value)
            => SetStateFlag(a, value, VisorToggledFlag);

        public static unsafe ref float Alpha(this Actor a)
            => ref *(float*) ((byte*) a.Address + AlphaOffset);
    }
}
