using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Glamourer.Customization
{
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
            MultiIconSelector = 4,
            Percentage        = 5,
        }

        public struct Menu
        {
            public uint            Id;
            public byte            InitVal;
            public MenuType        Type;
            public byte            Size;
            public byte            LookAt;
            public uint            Mask;
            public CustomizationId Customization;
            public uint[]          Values;
            public byte[]          Graphic;
        }

        public struct FacialFeatures
        {
            public uint[] Icons;
        }

        public LazyRow<Race>  Race  { get; set; } = null!;
        public LazyRow<Tribe> Tribe { get; set; } = null!;

        public sbyte Gender { get; set; }

        public Menu[]                              Menus               { get; set; } = new Menu[NumMenus];
        public byte[]                              Voices              { get; set; } = new byte[NumVoices];
        public FacialFeatures[]                    FacialFeatureByFace { get; set; } = new FacialFeatures[NumFaces];
        public CharaMakeType.CharaMakeTypeUnkData3347Obj[] Equip               { get; set; } = new CharaMakeType.CharaMakeTypeUnkData3347Obj[NumEquip];

        public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language)
        {
            RowId    = parser.RowId;
            SubRowId = parser.SubRowId;
            Race     = new LazyRow<Race>(gameData, parser.ReadColumn<uint>(0), language);
            Tribe    = new LazyRow<Tribe>(gameData, parser.ReadColumn<uint>(1), language);
            Gender   = parser.ReadColumn<sbyte>(2);
            for (var i = 0; i < NumMenus; ++i)
            {
                Menus[i].Id            = parser.ReadColumn<uint>(3 + 0 * NumMenus + i);
                Menus[i].InitVal       = parser.ReadColumn<byte>(3 + 1 * NumMenus + i);
                Menus[i].Type          = (MenuType) parser.ReadColumn<byte>(3 + 2 * NumMenus + i);
                Menus[i].Size          = parser.ReadColumn<byte>(3 + 3 * NumMenus + i);
                Menus[i].LookAt        = parser.ReadColumn<byte>(3 + 4 * NumMenus + i);
                Menus[i].Mask          = parser.ReadColumn<uint>(3 + 5 * NumMenus + i);
                Menus[i].Customization = (CustomizationId) parser.ReadColumn<uint>(3 + 6 * NumMenus + i);
                Menus[i].Values        = new uint[Menus[i].Size];

                switch (Menus[i].Type)
                {
                    case MenuType.ColorPicker:
                    case MenuType.DoubleColorPicker:
                    case MenuType.Percentage:
                        break;
                    default:
                        for (var j = 0; j < Menus[i].Size; ++j)
                            Menus[i].Values[j] = parser.ReadColumn<uint>(3 + (7 + j) * NumMenus + i);
                        break;
                }

                Menus[i].Graphic = new byte[NumGraphics];
                for (var j = 0; j < NumGraphics; ++j)
                    Menus[i].Graphic[j] = parser.ReadColumn<byte>(3 + (MaxNumValues + 7 + j) * NumMenus + i);
            }

            for (var i = 0; i < NumVoices; ++i)
                Voices[i] = parser.ReadColumn<byte>(3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + i);

            for (var i = 0; i < NumFaces; ++i)
            {
                FacialFeatureByFace[i].Icons = new uint[NumFeatures];
                for (var j = 0; j < NumFeatures; ++j)
                    FacialFeatureByFace[i].Icons[j] =
                        (uint) parser.ReadColumn<int>(3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + NumVoices + j * NumFaces + i);
            }

            for (var i = 0; i < NumEquip; ++i)
            {
                Equip[i] = new CharaMakeType.CharaMakeTypeUnkData3347Obj()
                {
                    Helmet = parser.ReadColumn<ulong>(
                        3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + NumVoices + NumFaces * NumFeatures + i * 7 + 0),
                    Top = parser.ReadColumn<ulong>(
                        3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + NumVoices + NumFaces * NumFeatures + i * 7 + 1),
                    Gloves = parser.ReadColumn<ulong>(
                        3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + NumVoices + NumFaces * NumFeatures + i * 7 + 2),
                    Legs = parser.ReadColumn<ulong>(
                        3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + NumVoices + NumFaces * NumFeatures + i * 7 + 3),
                    Shoes = parser.ReadColumn<ulong>(
                        3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + NumVoices + NumFaces * NumFeatures + i * 7 + 4),
                    Weapon = parser.ReadColumn<ulong>(
                        3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + NumVoices + NumFaces * NumFeatures + i * 7 + 5),
                    SubWeapon = parser.ReadColumn<ulong>(
                        3 + (MaxNumValues + 7 + NumGraphics) * NumMenus + NumVoices + NumFaces * NumFeatures + i * 7 + 6),
                };
            }
        }
    }
}
