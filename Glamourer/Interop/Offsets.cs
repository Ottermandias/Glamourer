namespace Glamourer.Interop;

public static class Offsets
{
    public static class Character
    {
        public const int Wetness = 0x1ADA;
        public const int HatVisible = 0x84E;
        public const int VisorToggled = 0x84F;
        public const int WeaponHidden1 = 0x84F;
        public const int WeaponHidden2 = 0x72C;
        public const int Alpha = 0x19E0;

        public static class Flags
        {
            public const byte IsHatHidden = 0x01;
            public const byte IsVisorToggled = 0x08;
            public const byte IsWet = 0x80;
            public const byte IsWeaponHidden1 = 0x01;
            public const byte IsWeaponHidden2 = 0x02;
        }
    }
}
