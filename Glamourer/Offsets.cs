namespace Glamourer;

public static class Offsets
{
    public static class Character
    {
        public const int Wetness       = 0x1B3A;
        public const int HatVisible    = 0x876;
        public const int VisorToggled  = 0x877;
        public const int WeaponHidden1 = 0x877;
        public const int WeaponHidden2 = 0x754;
        public const int Alpha         = 0x1A4C;

        public static class Flags
        {
            public const byte IsHatHidden     = 0x01;
            public const byte IsVisorToggled  = 0x08;
            public const byte IsWet           = 0x20;
            public const byte IsWeaponHidden1 = 0x01;
            public const byte IsWeaponHidden2 = 0x02;
        }
    }
}
