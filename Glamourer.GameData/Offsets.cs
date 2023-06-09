namespace Glamourer;

public static class Offsets
{
    public static class Character
    {
        public const int ClassJobContainer = 0x1A8;
    }

    public const byte DrawObjectVisorStateFlag  = 0x40;
    public const byte DrawObjectVisorToggleFlag = 0x80;
}

public static class Sigs
{
    public const string ChangeJob         = "88 51 ?? 44 3B CA";
    public const string FlagSlotForUpdate = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A";
    public const string ChangeCustomize   = "E8 ?? ?? ?? ?? 41 0F B6 C5 66 41 89 86";
}
