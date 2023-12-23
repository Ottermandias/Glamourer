using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Glamourer.GameData;

/// <summary> A custom version of CharaMakeParams that is easier to parse. </summary>
[Sheet("CharaMakeParams")]
public class CharaMakeParams : ExcelRow
{
    public const int NumMenus     = 28;
    public const int NumVoices    = 12;
    public const int NumGraphics  = 10;
    public const int MaxNumValues = 100;
    public const int NumFaces     = 8;
    public const int NumFeatures  = 7;
    public const int NumEquip     = 3;

    public enum MenuType
    {
        ListSelector      = 0,
        IconSelector      = 1,
        ColorPicker       = 2,
        DoubleColorPicker = 3,
        IconCheckmark     = 4,
        Percentage        = 5,
        Checkmark         = 6, // custom
        Nothing           = 7, // custom
        List1Selector     = 8, // custom, 1-indexed lists
    }

    public struct Menu
    {
        public uint     Id;
        public byte     InitVal;
        public MenuType Type;
        public byte     Size;
        public byte     LookAt;
        public uint     Mask;
        public uint     Customize;
        public uint[]   Values;
        public byte[]   Graphic;
    }

    public struct FacialFeatures
    {
        public uint[] Icons;
    }

    public LazyRow<Race>  Race   { get; set; } = null!;
    public LazyRow<Tribe> Tribe  { get; set; } = null!;
    public sbyte          Gender { get; set; }

    public Menu[]           Menus               { get; set; } = new Menu[NumMenus];
    public byte[]           Voices              { get; set; } = new byte[NumVoices];
    public FacialFeatures[] FacialFeatureByFace { get; set; } = new FacialFeatures[NumFaces];

    public CharaMakeType.CharaMakeTypeUnkData3347Obj[] Equip { get; set; } = new CharaMakeType.CharaMakeTypeUnkData3347Obj[NumEquip];

    public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language)
    {
        RowId    = parser.RowId;
        SubRowId = parser.SubRowId;
        Race     = new LazyRow<Race>(gameData, parser.ReadColumn<uint>(0), language);
        Tribe    = new LazyRow<Tribe>(gameData, parser.ReadColumn<uint>(1), language);
        Gender   = parser.ReadColumn<sbyte>(2);
        int currentOffset;
        for (var i = 0; i < NumMenus; ++i)
        {
            currentOffset      = 3 + i;
            Menus[i].Id        = parser.ReadColumn<uint>(0 * NumMenus + currentOffset);
            Menus[i].InitVal   = parser.ReadColumn<byte>(1 * NumMenus + currentOffset);
            Menus[i].Type      = (MenuType)parser.ReadColumn<byte>(2 * NumMenus + currentOffset);
            Menus[i].Size      = parser.ReadColumn<byte>(3 * NumMenus + currentOffset);
            Menus[i].LookAt    = parser.ReadColumn<byte>(4 * NumMenus + currentOffset);
            Menus[i].Mask      = parser.ReadColumn<uint>(5 * NumMenus + currentOffset);
            Menus[i].Customize = parser.ReadColumn<uint>(6 * NumMenus + currentOffset);
            Menus[i].Values    = new uint[Menus[i].Size];

            switch (Menus[i].Type)
            {
                case MenuType.ColorPicker:
                case MenuType.DoubleColorPicker:
                case MenuType.Percentage:
                    break;
                default:
                    currentOffset += 7 * NumMenus;
                    for (var j = 0; j < Menus[i].Size; ++j)
                        Menus[i].Values[j] = parser.ReadColumn<uint>(j * NumMenus + currentOffset);
                    break;
            }

            Menus[i].Graphic = new byte[NumGraphics];
            currentOffset    = 3 + (MaxNumValues + 7) * NumMenus + i;
            for (var j = 0; j < NumGraphics; ++j)
                Menus[i].Graphic[j] = parser.ReadColumn<byte>(j * NumMenus + currentOffset);
        }

        currentOffset = 3 + (MaxNumValues + 7 + NumGraphics) * NumMenus;
        for (var i = 0; i < NumVoices; ++i)
            Voices[i] = parser.ReadColumn<byte>(currentOffset++);

        for (var i = 0; i < NumFaces; ++i)
        {
            currentOffset                = 3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + NumVoices + i;
            FacialFeatureByFace[i].Icons = new uint[NumFeatures];
            for (var j = 0; j < NumFeatures; ++j)
                FacialFeatureByFace[i].Icons[j] = (uint)parser.ReadColumn<int>(j * NumFaces + currentOffset);
        }

        for (var i = 0; i < NumEquip; ++i)
        {
            currentOffset = 3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + NumVoices + NumFaces * NumFeatures + i * 7;
            Equip[i] = new CharaMakeType.CharaMakeTypeUnkData3347Obj()
            {
                Helmet    = parser.ReadColumn<ulong>(currentOffset + 0),
                Top       = parser.ReadColumn<ulong>(currentOffset + 1),
                Gloves    = parser.ReadColumn<ulong>(currentOffset + 2),
                Legs      = parser.ReadColumn<ulong>(currentOffset + 3),
                Shoes     = parser.ReadColumn<ulong>(currentOffset + 4),
                Weapon    = parser.ReadColumn<ulong>(currentOffset + 5),
                SubWeapon = parser.ReadColumn<ulong>(currentOffset + 6),
            };
        }
    }
}
