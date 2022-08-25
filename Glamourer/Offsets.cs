namespace Glamourer;

public static class Offsets
{
    public static class Character
    {
        public const int Wetness       = 0x1AF3;
        public const int HatVisible    = 0x85E;
        public const int VisorToggled  = 0x85F;
        public const int WeaponHidden1 = 0x85F;
        public const int WeaponHidden2 = 0x73C;
        public const int Alpha         = 0x19F8;

        public static class Flags
        {
            public const byte IsHatHidden     = 0x01;
            public const byte IsVisorToggled  = 0x08;
            public const byte IsWet           = 0x40;
            public const byte IsWeaponHidden1 = 0x01;
            public const byte IsWeaponHidden2 = 0x02;
        }
    }
}
