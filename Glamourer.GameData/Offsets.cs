namespace Glamourer;

public static class Offsets
{
    public static class Character
    {
        public const int ClassJobContainer = 0x1A8;

        public const int Wetness       = 0x1ADA;
        public const int HatVisible    = 0x84E;
        public const int VisorToggled  = 0x84F;
        public const int WeaponHidden1 = 0x84F;
        public const int WeaponHidden2 = 0x72C;
        public const int Alpha         = 0x19E0;

        public static class Flags
        {
            public const byte IsHatHidden     = 0x01;
            public const byte IsVisorToggled  = 0x08;
            public const byte IsWet           = 0x80;
            public const byte IsWeaponHidden1 = 0x01;
            public const byte IsWeaponHidden2 = 0x02;
        }
    }

    public const byte DrawObjectVisorStateFlag  = 0x40;
    public const byte DrawObjectVisorToggleFlag = 0x80;
}

public static class Sigs
{
    public const string ChangeJob         = "88 51 ?? 44 3B CA";
    public const string FlagSlotForUpdate = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A";
}
