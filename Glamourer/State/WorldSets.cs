using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class WorldSets
{
    // @formatter:off
    private static readonly Dictionary<(Gender, Race), FunEquipSet.Group> Starter = new()
    {
        [(Gender.Male,   Race.Hyur)]     = FunEquipSet.Group.FullSetWithoutHat(0084, 2),
        [(Gender.Female, Race.Hyur)]     = FunEquipSet.Group.FullSetWithoutHat(0085, 2),
        [(Gender.Male,   Race.Elezen)]   = FunEquipSet.Group.FullSetWithoutHat(0086, 2),
        [(Gender.Female, Race.Elezen)]   = FunEquipSet.Group.FullSetWithoutHat(0087, 2),
        [(Gender.Male,   Race.Miqote)]   = FunEquipSet.Group.FullSetWithoutHat(0088, 2),
        [(Gender.Female, Race.Miqote)]   = FunEquipSet.Group.FullSetWithoutHat(0089, 2),
        [(Gender.Male,   Race.Roegadyn)] = FunEquipSet.Group.FullSetWithoutHat(0090, 2),
        [(Gender.Female, Race.Roegadyn)] = FunEquipSet.Group.FullSetWithoutHat(0091, 2),
        [(Gender.Male,   Race.Lalafell)] = FunEquipSet.Group.FullSetWithoutHat(0092, 2),
        [(Gender.Female, Race.Lalafell)] = FunEquipSet.Group.FullSetWithoutHat(0093, 2),
        [(Gender.Male,   Race.AuRa)]     = FunEquipSet.Group.FullSetWithoutHat(0257, 2),
        [(Gender.Female, Race.AuRa)]     = FunEquipSet.Group.FullSetWithoutHat(0258, 2),
        [(Gender.Male,   Race.Hrothgar)] = FunEquipSet.Group.FullSetWithoutHat(0597, 1),
        [(Gender.Female, Race.Hrothgar)] = FunEquipSet.Group.FullSetWithoutHat(0000, 0), // TODO Hrothgar Female
        [(Gender.Male,   Race.Viera)]    = FunEquipSet.Group.FullSetWithoutHat(0744, 1),
        [(Gender.Female, Race.Viera)]    = FunEquipSet.Group.FullSetWithoutHat(0581, 1),
    };

    private static readonly (CharacterWeapon, CharacterWeapon)[] StarterWeapons = 
    {
        (CharacterWeapon.Empty,             CharacterWeapon.Empty),            // ADV,
        (CharacterWeapon.Int(0201, 43, 01), CharacterWeapon.Int(0101, 07, 2)), // GLA, Weathered Shortsword, Square Maple
        (CharacterWeapon.Int(0301, 09, 01), CharacterWeapon.Int(0351, 09, 1)), // PGL, Weathered Hora
        (CharacterWeapon.Int(0401, 31, 07), CharacterWeapon.Empty),            // MRD, Weathered War Axe
        (CharacterWeapon.Int(0501, 03, 12), CharacterWeapon.Empty),            // LNC, Weathered Spear
        (CharacterWeapon.Int(0601, 01, 08), CharacterWeapon.Int(0698, 01, 2)), // ARC, Weathered Shortbow
        (CharacterWeapon.Int(0801, 01, 12), CharacterWeapon.Empty),            // CNJ, Weathered Cane
        (CharacterWeapon.Int(1001, 01, 01), CharacterWeapon.Empty),            // THM, Bone Staff
        (CharacterWeapon.Int(5001, 01, 01), CharacterWeapon.Int(5041, 01, 1)), // CRP, Weathered, Amateur's
        (CharacterWeapon.Int(5101, 02, 01), CharacterWeapon.Int(5141, 01, 1)), // BSM, Weathered, Amateur's
        (CharacterWeapon.Int(5201, 01, 05), CharacterWeapon.Int(5241, 01, 1)), // ARM, Weathered, Amateur's
        (CharacterWeapon.Int(5301, 01, 01), CharacterWeapon.Int(5341, 01, 1)), // GSM, Weathered, Amateur's
        (CharacterWeapon.Int(5401, 01, 06), CharacterWeapon.Int(5441, 01, 1)), // LTW, Weathered, Amateur's
        (CharacterWeapon.Int(5501, 01, 14), CharacterWeapon.Int(5571, 01, 1)), // WVR, Rusty, Amateur's
        (CharacterWeapon.Int(5601, 01, 04), CharacterWeapon.Int(5641, 01, 1)), // ALC, Weathered, Amateur's
        (CharacterWeapon.Int(5701, 01, 07), CharacterWeapon.Int(5741, 01, 1)), // CUL, Weathered, Amateur's
        (CharacterWeapon.Int(7001, 01, 05), CharacterWeapon.Int(7051, 01, 1)), // MIN, Weathered, Amateur's
        (CharacterWeapon.Int(7101, 01, 05), CharacterWeapon.Int(7151, 01, 1)), // BTN, Weathered, Amateur's
        (CharacterWeapon.Int(7201, 01, 06), CharacterWeapon.Int(7255, 01, 1)), // FSH, Weathered, Gig
        (CharacterWeapon.Int(0201, 43, 01), CharacterWeapon.Int(0101, 07, 2)), // PLD, Weathered Shortsword, Square Maple
        (CharacterWeapon.Int(0301, 09, 01), CharacterWeapon.Int(0351, 09, 1)), // MNK, Weathered Hora
        (CharacterWeapon.Int(0401, 31, 07), CharacterWeapon.Empty),            // WAR, Weathered War Axe
        (CharacterWeapon.Int(0501, 03, 12), CharacterWeapon.Empty),            // DRG, Weathered Spear
        (CharacterWeapon.Int(0601, 01, 08), CharacterWeapon.Int(0698, 01, 2)), // BRD, Weathered Shortbow
        (CharacterWeapon.Int(0801, 01, 12), CharacterWeapon.Empty),            // WHM, Weathered Cane
        (CharacterWeapon.Int(1001, 01, 01), CharacterWeapon.Empty),            // BLM, Bone Staff
        (CharacterWeapon.Int(1708, 01, 01), CharacterWeapon.Empty),            // ACN, Weathered Grimoire
        (CharacterWeapon.Int(1708, 01, 01), CharacterWeapon.Empty),            // SMN, Weathered Grimoire
        (CharacterWeapon.Int(1708, 01, 01), CharacterWeapon.Empty),            // SCH, Weathered Grimoire
        (CharacterWeapon.Int(1801, 33, 07), CharacterWeapon.Int(1851, 33, 7)), // ROG, Weathered Daggers
        (CharacterWeapon.Int(1801, 33, 07), CharacterWeapon.Int(1851, 33, 7)), // NIN, Weathered Daggers
        (CharacterWeapon.Int(2001, 11, 01), CharacterWeapon.Int(2099, 01, 1)), // MCH, Steel-barreled Carbine
        (CharacterWeapon.Int(1501, 01, 05), CharacterWeapon.Empty),            // DRK, Steel Claymore
        (CharacterWeapon.Int(2101, 07, 01), CharacterWeapon.Int(2199, 01, 1)), // AST, Star Globe
        (CharacterWeapon.Int(2201, 26, 01), CharacterWeapon.Int(2251, 38, 1)), // SAM, Mythrite Uchigatana
        (CharacterWeapon.Int(2301, 38, 01), CharacterWeapon.Int(2351, 29, 1)), // RDM, Mythrite Rapier
        (CharacterWeapon.Int(2401, 02, 01), CharacterWeapon.Empty),            // BLU, Rainmaker
        (CharacterWeapon.Int(2501, 14, 01), CharacterWeapon.Empty),            // GNB, High Steel Gunblade
        (CharacterWeapon.Int(2601, 13, 01), CharacterWeapon.Int(2651, 13, 1)), // DNC, High Steel Chakrams
        (CharacterWeapon.Int(2802, 13, 01), CharacterWeapon.Empty),            // RPR, Deepgold War Scythe
        (CharacterWeapon.Int(2702, 08, 01), CharacterWeapon.Empty),            // SGE, Stonegold Milpreves
    };

    private static readonly (FunEquipSet.Group, CharacterWeapon, CharacterWeapon)[] _50Artifact = 
    {
        (FunEquipSet.Group.FullSet(000, 0),                                CharacterWeapon.Empty,            CharacterWeapon.Empty),            // ADV, Nothing
        (FunEquipSet.Group.FullSet(042, 1),                                CharacterWeapon.Int(0201, 10, 1), CharacterWeapon.Int(0101, 11, 1)), // GLA, Gallant, Curtana, Holy Shield
        (FunEquipSet.Group.FullSet(044, 1),                                CharacterWeapon.Int(0304, 01, 1), CharacterWeapon.Int(0354, 01, 1)), // PGL, Temple, Sphairai
        (FunEquipSet.Group.FullSet(037, 1),                                CharacterWeapon.Int(0401, 07, 1), CharacterWeapon.Empty),            // MRD, Fighter's, Bravura
        (FunEquipSet.Group.FullSet(036, 1),                                CharacterWeapon.Int(0504, 01, 1), CharacterWeapon.Empty),            // LNC, Drachen, Gae Bolg
        (FunEquipSet.Group.FullSet(041, 1),                                CharacterWeapon.Int(0603, 01, 1), CharacterWeapon.Int(0698, 04, 1)), // ARC, Choral, Artemis
        (FunEquipSet.Group.FullSet(039, 1),                                CharacterWeapon.Int(0801, 06, 1), CharacterWeapon.Empty),            // CNJ, Healer's, Thyrus
        (FunEquipSet.Group.FullSet(040, 1),                                CharacterWeapon.Int(1001, 02, 1), CharacterWeapon.Empty),            // THM, Wizard's, Stardust Rod
        (FunEquipSet.Group.FullSet(074, 1),                                CharacterWeapon.Int(5003, 01, 1), CharacterWeapon.Int(5041, 01, 4)), // CRP, Militia, Ullikummi
        (FunEquipSet.Group.FullSet(073, 1),                                CharacterWeapon.Int(5101, 02, 1), CharacterWeapon.Int(5141, 01, 4)), // BSM, Militia, Vulcan
        (FunEquipSet.Group.FullSet(076, 1),                                CharacterWeapon.Int(5201, 04, 1), CharacterWeapon.Int(5241, 01, 4)), // ARM, Militia, Kurdalegon
        (FunEquipSet.Group.FullSet(078, 1),                                CharacterWeapon.Int(5301, 03, 1), CharacterWeapon.Int(5341, 01, 1)), // GSM, Militia, Urcaguary
        (FunEquipSet.Group.FullSet(077, 1),                                CharacterWeapon.Int(5401, 05, 1), CharacterWeapon.Int(5441, 01, 4)), // LTW, Militia, Pinga
        (FunEquipSet.Group.FullSet(075, 1),                                CharacterWeapon.Int(5501, 02, 1), CharacterWeapon.Int(5571, 01, 1)), // WVR, Militia, Clotho
        (FunEquipSet.Group.FullSet(079, 1),                                CharacterWeapon.Int(5603, 01, 1), CharacterWeapon.Int(5641, 01, 4)), // ALC, Militia, Paracelsus
        (FunEquipSet.Group.FullSet(080, 1),                                CharacterWeapon.Int(5701, 04, 1), CharacterWeapon.Int(5741, 01, 4)), // CUL, Militia, Chantico
        (FunEquipSet.Group.FullSet(072, 1),                                CharacterWeapon.Int(7002, 01, 1), CharacterWeapon.Int(7051, 01, 4)), // MIN, Militia, Mammon
        (FunEquipSet.Group.FullSet(071, 1),                                CharacterWeapon.Int(7101, 05, 1), CharacterWeapon.Int(7151, 01, 4)), // BTN, Militia, Rauni
        (FunEquipSet.Group.FullSet(070, 1),                                CharacterWeapon.Int(7201, 04, 1), CharacterWeapon.Int(7255, 01, 1)), // FSH, Militia, Halcyon
        (FunEquipSet.Group.FullSet(042, 1),                                CharacterWeapon.Int(0201, 10, 1), CharacterWeapon.Int(0101, 11, 1)), // PLD, Gallant, Curtana, Holy Shield
        (FunEquipSet.Group.FullSet(044, 1),                                CharacterWeapon.Int(0304, 01, 1), CharacterWeapon.Int(0354, 01, 1)), // MNK, Temple, Sphairai
        (FunEquipSet.Group.FullSet(037, 1),                                CharacterWeapon.Int(0401, 07, 1), CharacterWeapon.Empty),            // WAR, Fighter's, Bravura
        (FunEquipSet.Group.FullSet(036, 1),                                CharacterWeapon.Int(0504, 01, 1), CharacterWeapon.Empty),            // DRG, Drachen, Gae Bolg
        (FunEquipSet.Group.FullSet(041, 1),                                CharacterWeapon.Int(0603, 01, 1), CharacterWeapon.Int(0698, 04, 1)), // BRD, Choral, Artemis
        (FunEquipSet.Group.FullSet(039, 1),                                CharacterWeapon.Int(0801, 06, 1), CharacterWeapon.Empty),            // WHM, Healer's, Thyrus
        (FunEquipSet.Group.FullSet(040, 1),                                CharacterWeapon.Int(1001, 02, 1), CharacterWeapon.Empty),            // BLM, Wizard's, Stardust Rod
        (FunEquipSet.Group.FullSet(132, 1),                                CharacterWeapon.Int(1705, 01, 1), CharacterWeapon.Empty),            // ACN, Evoker's, Veil of Wiyu
        (FunEquipSet.Group.FullSet(132, 1),                                CharacterWeapon.Int(1705, 01, 1), CharacterWeapon.Empty),            // SMN, Evoker's, Veil of Wiyu
        (FunEquipSet.Group.FullSet(133, 1),                                CharacterWeapon.Int(1715, 01, 1), CharacterWeapon.Empty),            // SCH, Scholar's
        (FunEquipSet.Group.FullSet(170, 1),                                CharacterWeapon.Int(1801, 02, 1), CharacterWeapon.Int(1851, 02, 1)), // ROG, Ninja, Yoshimitsu
        (FunEquipSet.Group.FullSet(170, 1),                                CharacterWeapon.Int(1801, 02, 1), CharacterWeapon.Int(1851, 02, 1)), // NIN, Ninja, Yoshimitsu
        (FunEquipSet.Group.FullSet(265, 1),                                CharacterWeapon.Int(2001, 11, 1), CharacterWeapon.Int(2099, 01, 1)), // MCH, Machinist's, Steel-barreled Carbine
        (FunEquipSet.Group.FullSet(264, 1),                                CharacterWeapon.Int(1501, 01, 5), CharacterWeapon.Empty),            // DRK, Chaos, Steel Claymore
        (FunEquipSet.Group.FullSet(266, 1),                                CharacterWeapon.Int(2101, 07, 1), CharacterWeapon.Int(2199, 01, 1)), // AST, Welkin, Star Globe
        (new FunEquipSet.Group(246, 5, 489, 1, 246, 5, 246, 5, 245, 5),    CharacterWeapon.Int(2201, 26, 1), CharacterWeapon.Int(2251, 38, 1)), // SAM, Nameless, Mythrite Uchigatana
        (new FunEquipSet.Group(16, 160, 16, 54, 103, 1, 16, 105, 16, 167), CharacterWeapon.Int(2301, 38, 1), CharacterWeapon.Int(2351, 29, 1)), // RDM, Red, Mythrite Rapier
        (new FunEquipSet.Group(9, 255, 338, 6, 338, 6, 338, 6, 338, 6),    CharacterWeapon.Int(2401, 02, 1), CharacterWeapon.Empty),            // BLU, True Blue, Rainmaker
        (new FunEquipSet.Group(592, 3, 380, 4, 380, 4, 380, 4, 380, 4),    CharacterWeapon.Int(2501, 14, 1), CharacterWeapon.Empty),            // GNB, Outsider's, High Steel Gunblade
        (FunEquipSet.Group.FullSet(204, 4),                                CharacterWeapon.Int(2601, 13, 1), CharacterWeapon.Int(2651, 13, 1)), // DNC, Softstepper, High Steel Chakrams
        (new FunEquipSet.Group(206, 7, 303, 3, 23, 109, 303, 3, 262, 7),   CharacterWeapon.Int(2802, 13, 1), CharacterWeapon.Empty),            // RPR, Muzhik, Deepgold War Scythe
        (new FunEquipSet.Group(20, 46, 289, 6, 342, 3, 120, 9, 342, 3),    CharacterWeapon.Int(2702, 08, 1), CharacterWeapon.Empty),            // SGE, Bookwyrm, Stonegold Milpreves
    };

    private static readonly (FunEquipSet.Group, CharacterWeapon, CharacterWeapon)[] _60Artifact = 
    {
        (FunEquipSet.Group.FullSet(000, 0),                                CharacterWeapon.Empty,            CharacterWeapon.Empty),             // ADV, Nothing
        (FunEquipSet.Group.FullSet(160, 1),                                CharacterWeapon.Int(0201, 30, 1), CharacterWeapon.Int(0101, 29, 01)), // GLA, Creed, Hauteclaire, Prytwen
        (FunEquipSet.Group.FullSet(161, 1),                                CharacterWeapon.Int(0313, 02, 1), CharacterWeapon.Int(0363, 02, 01)), // PGL, Tantra, Rising Suns
        (FunEquipSet.Group.FullSet(162, 1),                                CharacterWeapon.Int(0401, 20, 1), CharacterWeapon.Empty),             // MRD, Ravager, Parashu
        (FunEquipSet.Group.FullSet(163, 1),                                CharacterWeapon.Int(0501, 18, 1), CharacterWeapon.Empty),             // LNC, Dragonlancer, Brionac
        (FunEquipSet.Group.FullSet(164, 1),                                CharacterWeapon.Int(0601, 24, 1), CharacterWeapon.Int(0698, 23, 01)), // ARC, Aoidos, Berimbau
        (FunEquipSet.Group.FullSet(165, 1),                                CharacterWeapon.Int(0801, 18, 1), CharacterWeapon.Empty),             // CNJ, Orison, Seraph Cane
        (FunEquipSet.Group.FullSet(166, 1),                                CharacterWeapon.Int(1001, 13, 1), CharacterWeapon.Empty),             // THM, Goetia, Lunaris Rod
        (FunEquipSet.Group.FullSet(209, 1),                                CharacterWeapon.Int(5004, 01, 1), CharacterWeapon.Int(5041, 01, 01)), // CRP, Millkeep's
        (FunEquipSet.Group.FullSet(210, 1),                                CharacterWeapon.Int(5101, 03, 1), CharacterWeapon.Int(5141, 01, 07)), // BSM, Forgekeep's
        (FunEquipSet.Group.FullSet(211, 1),                                CharacterWeapon.Int(5201, 05, 1), CharacterWeapon.Int(5241, 01, 07)), // ARM, Hammerkeep's
        (FunEquipSet.Group.FullSet(212, 1),                                CharacterWeapon.Int(5301, 04, 1), CharacterWeapon.Int(5341, 01, 01)), // GSM, Gemkeep's
        (FunEquipSet.Group.FullSet(213, 1),                                CharacterWeapon.Int(5403, 01, 1), CharacterWeapon.Int(5441, 01, 06)), // LTW, Hidekeep's
        (FunEquipSet.Group.FullSet(214, 1),                                CharacterWeapon.Int(5501, 03, 1), CharacterWeapon.Int(5571, 01, 01)), // WVR, Boltkeep's
        (FunEquipSet.Group.FullSet(215, 1),                                CharacterWeapon.Int(5603, 02, 1), CharacterWeapon.Int(5641, 01, 07)), // ALC, Cauldronkeep's
        (FunEquipSet.Group.FullSet(216, 1),                                CharacterWeapon.Int(5701, 07, 1), CharacterWeapon.Int(5741, 01, 07)), // CUL, Galleykeep's
        (FunEquipSet.Group.FullSet(217, 1),                                CharacterWeapon.Int(7003, 01, 1), CharacterWeapon.Int(7051, 01, 07)), // MIN, Minekeep's
        (FunEquipSet.Group.FullSet(218, 1),                                CharacterWeapon.Int(7101, 07, 1), CharacterWeapon.Int(7151, 01, 07)), // BTN, Fieldkeep's
        (FunEquipSet.Group.FullSet(219, 1),                                CharacterWeapon.Int(7201, 05, 1), CharacterWeapon.Int(7255, 01, 01)), // FSH, Tacklekeep's
        (FunEquipSet.Group.FullSet(160, 1),                                CharacterWeapon.Int(0201, 10, 1), CharacterWeapon.Int(0101, 11, 01)), // PLD, Creed 
        (FunEquipSet.Group.FullSet(161, 1),                                CharacterWeapon.Int(0304, 01, 1), CharacterWeapon.Int(0354, 01, 01)), // MNK, Tantra 
        (FunEquipSet.Group.FullSet(162, 1),                                CharacterWeapon.Int(0401, 07, 1), CharacterWeapon.Empty),             // WAR, Ravager 
        (FunEquipSet.Group.FullSet(163, 1),                                CharacterWeapon.Int(0504, 01, 1), CharacterWeapon.Empty),             // DRG, Dragonlancer 
        (FunEquipSet.Group.FullSet(164, 1),                                CharacterWeapon.Int(0603, 01, 1), CharacterWeapon.Int(0698, 04, 01)), // BRD, Aoidos 
        (FunEquipSet.Group.FullSet(165, 1),                                CharacterWeapon.Int(0801, 06, 1), CharacterWeapon.Empty),             // WHM, Orison 
        (FunEquipSet.Group.FullSet(166, 1),                                CharacterWeapon.Int(1001, 02, 1), CharacterWeapon.Empty),             // BLM, Goetia 
        (FunEquipSet.Group.FullSet(167, 1),                                CharacterWeapon.Int(1718, 01, 1), CharacterWeapon.Empty),             // ACN, Caller, Almandal
        (FunEquipSet.Group.FullSet(167, 1),                                CharacterWeapon.Int(1718, 01, 1), CharacterWeapon.Empty),             // SMN, Caller, Almandal
        (FunEquipSet.Group.FullSet(168, 1),                                CharacterWeapon.Int(1701, 14, 1), CharacterWeapon.Empty),             // SCH, Savant, Elements
        (FunEquipSet.Group.FullSet(169, 1),                                CharacterWeapon.Int(1801, 29, 1), CharacterWeapon.Int(1851, 29, 01)), // ROG, Iga, Yukimitsu
        (FunEquipSet.Group.FullSet(169, 1),                                CharacterWeapon.Int(1801, 29, 1), CharacterWeapon.Int(1851, 29, 01)), // NIN, Iga, Yukimitsu
        (FunEquipSet.Group.FullSet(265, 1),                                CharacterWeapon.Int(2007, 01, 1), CharacterWeapon.Int(2099, 01, 01)), // MCH, Machinist's, Ferdinant
        (FunEquipSet.Group.FullSet(264, 1),                                CharacterWeapon.Int(1501, 37, 1), CharacterWeapon.Empty),             // DRK, Chaos, Deathbringer
        (FunEquipSet.Group.FullSet(266, 1),                                CharacterWeapon.Int(2104, 01, 1), CharacterWeapon.Int(2199, 01, 31)), // AST, Welkin, Atlas
        (new FunEquipSet.Group(246, 5, 489, 1, 246, 5, 246, 5, 245, 5),    CharacterWeapon.Int(2201, 26, 1), CharacterWeapon.Int(2251, 38, 01)), // SAM, Nameless, Mythrite Uchigatana
        (new FunEquipSet.Group(16, 160, 16, 54, 103, 1, 16, 105, 16, 167), CharacterWeapon.Int(2301, 38, 1), CharacterWeapon.Int(2351, 29, 01)), // RDM, Red, Mythrite Rapier
        (FunEquipSet.Group.FullSet(593, 1),                                CharacterWeapon.Int(2401, 01, 1), CharacterWeapon.Empty),             // BLU, Magus, Spirit of Whalaqee
        (new FunEquipSet.Group(592, 3, 380, 4, 380, 4, 380, 4, 380, 4),    CharacterWeapon.Int(2501, 14, 1), CharacterWeapon.Empty),             // GNB, Outsider's, High Steel Gunblade
        (FunEquipSet.Group.FullSet(204, 4),                                CharacterWeapon.Int(2601, 13, 1), CharacterWeapon.Int(2651, 13, 01)), // DNC, Softstepper, High Steel Chakrams
        (new FunEquipSet.Group(206, 7, 303, 3, 23, 109, 303, 3, 262, 7),   CharacterWeapon.Int(2802, 13, 1), CharacterWeapon.Empty),             // RPR, Muzhik, Deepgold War Scythe
        (new FunEquipSet.Group(20, 46, 289, 6, 342, 3, 120, 9, 342, 3),    CharacterWeapon.Int(2702, 08, 1), CharacterWeapon.Empty),             // SGE, Bookwyrm, Stonegold Milpreves
    };

    private static readonly (FunEquipSet.Group, CharacterWeapon, CharacterWeapon)[] _70Artifact = 
    {
        (FunEquipSet.Group.FullSet(000, 0),                                CharacterWeapon.Empty,            CharacterWeapon.Empty),             // ADV, Nothing
        (FunEquipSet.Group.FullSet(318, 1),                                CharacterWeapon.Int(0201, 94, 1), CharacterWeapon.Int(0101, 65, 01)), // GLA, Chivalrous, Galatyn, Evalach
        (new FunEquipSet.Group(319, 1, 319, 1, 8806, 1, 319, 1, 319, 1),   CharacterWeapon.Int(1606, 01, 1), CharacterWeapon.Int(1606, 01, 01)), // PGL, Pacifist's, Sudarshana Chakra
        (FunEquipSet.Group.FullSet(320, 1),                                CharacterWeapon.Int(0401, 63, 1), CharacterWeapon.Empty),             // MRD, Brutal, Farsha
        (FunEquipSet.Group.FullSet(321, 1),                                CharacterWeapon.Int(0501, 64, 1), CharacterWeapon.Empty),             // LNC, Trueblood, Ryunohige
        (FunEquipSet.Group.FullSet(322, 1),                                CharacterWeapon.Int(0617, 01, 1), CharacterWeapon.Int(0698, 59, 01)), // ARC, Storyteller's, Failnaught
        (FunEquipSet.Group.FullSet(323, 1),                                CharacterWeapon.Int(0810, 01, 1), CharacterWeapon.Empty),             // CNJ, Seventh Heaven, Aymur
        (FunEquipSet.Group.FullSet(324, 1),                                CharacterWeapon.Int(1011, 01, 1), CharacterWeapon.Empty),             // THM, Seventh Hell, Vanargand
        (FunEquipSet.Group.FullSet(443, 1),                                CharacterWeapon.Int(5005, 01, 1), CharacterWeapon.Int(5041, 01, 05)), // CRP, Millking
        (FunEquipSet.Group.FullSet(444, 1),                                CharacterWeapon.Int(5101, 04, 1), CharacterWeapon.Int(5141, 01, 06)), // BSM, Forgeking
        (FunEquipSet.Group.FullSet(445, 1),                                CharacterWeapon.Int(5201, 06, 1), CharacterWeapon.Int(5241, 01, 06)), // ARM, Hammerking
        (FunEquipSet.Group.FullSet(446, 1),                                CharacterWeapon.Int(5301, 05, 1), CharacterWeapon.Int(5341, 01, 01)), // GSM, Gemking
        (FunEquipSet.Group.FullSet(447, 1),                                CharacterWeapon.Int(5401, 06, 1), CharacterWeapon.Int(5441, 01, 05)), // LTW, Hideking
        (FunEquipSet.Group.FullSet(448, 1),                                CharacterWeapon.Int(5501, 04, 1), CharacterWeapon.Int(5571, 01, 01)), // WVR, Boltking
        (FunEquipSet.Group.FullSet(449, 1),                                CharacterWeapon.Int(5603, 03, 1), CharacterWeapon.Int(5641, 01, 06)), // ALC, Cauldronking
        (FunEquipSet.Group.FullSet(450, 1),                                CharacterWeapon.Int(5701, 07, 1), CharacterWeapon.Int(5741, 01, 05)), // CUL, Galleyking
        (FunEquipSet.Group.FullSet(451, 1),                                CharacterWeapon.Int(7001, 06, 1), CharacterWeapon.Int(7051, 01, 06)), // MIN, Mineking
        (FunEquipSet.Group.FullSet(452, 1),                                CharacterWeapon.Int(7102, 01, 1), CharacterWeapon.Int(7151, 01, 05)), // BTN, Fieldking
        (FunEquipSet.Group.FullSet(453, 1),                                CharacterWeapon.Int(7201, 06, 1), CharacterWeapon.Int(7255, 01, 01)), // FSH, Tackleking
        (FunEquipSet.Group.FullSet(318, 1),                                CharacterWeapon.Int(0201, 94, 1), CharacterWeapon.Int(0101, 65, 01)), // PLD, Chivalrous, Galatyn, Evalach
        (new FunEquipSet.Group(319, 1, 319, 1, 8806, 1, 319, 1, 319, 1),   CharacterWeapon.Int(1606, 01, 1), CharacterWeapon.Int(0363, 02, 01)), // MNK, Pacifist's, Sudarshana Chakra
        (FunEquipSet.Group.FullSet(320, 1),                                CharacterWeapon.Int(0401, 63, 1), CharacterWeapon.Empty),             // WAR, Brutal, Farsha
        (FunEquipSet.Group.FullSet(321, 1),                                CharacterWeapon.Int(0501, 64, 1), CharacterWeapon.Empty),             // DRG, Trueblood, Ryunohige
        (FunEquipSet.Group.FullSet(322, 1),                                CharacterWeapon.Int(0617, 01, 1), CharacterWeapon.Int(0698, 59, 01)), // BRD, Storyteller's, Failnaught
        (FunEquipSet.Group.FullSet(323, 1),                                CharacterWeapon.Int(0810, 01, 1), CharacterWeapon.Empty),             // WHM, Seventh Heaven, Aymur
        (FunEquipSet.Group.FullSet(324, 1),                                CharacterWeapon.Int(1011, 01, 1), CharacterWeapon.Empty),             // BLM, Seventh Hell, Vanargand
        (FunEquipSet.Group.FullSet(325, 1),                                CharacterWeapon.Int(1728, 01, 1), CharacterWeapon.Empty),             // ACN, Channeler, Lemegeton
        (FunEquipSet.Group.FullSet(325, 1),                                CharacterWeapon.Int(1728, 01, 1), CharacterWeapon.Empty),             // SMN, Channeler, Lemegeton
        (FunEquipSet.Group.FullSet(326, 1),                                CharacterWeapon.Int(1730, 01, 1), CharacterWeapon.Empty),             // SCH, Orator, Organum
        (FunEquipSet.Group.FullSet(327, 1),                                CharacterWeapon.Int(1801, 61, 1), CharacterWeapon.Int(1851, 61, 01)), // ROG, Kage-kakushi, Nagi
        (FunEquipSet.Group.FullSet(327, 1),                                CharacterWeapon.Int(1801, 61, 1), CharacterWeapon.Int(1851, 61, 01)), // NIN, Kage-kakushi, Nagi
        (FunEquipSet.Group.FullSet(329, 1),                                CharacterWeapon.Int(2001, 39, 1), CharacterWeapon.Int(2099, 01, 01)), // MCH, Gunner, Outsider
        (FunEquipSet.Group.FullSet(328, 1),                                CharacterWeapon.Int(1501, 60, 1), CharacterWeapon.Empty),             // DRK, Abyss, Caladbolg
        (FunEquipSet.Group.FullSet(330, 1),                                CharacterWeapon.Int(2109, 01, 1), CharacterWeapon.Int(2199, 01, 82)), // AST, Constellation, Pleiades
        (FunEquipSet.Group.FullSet(377, 1),                                CharacterWeapon.Int(2201, 12, 1), CharacterWeapon.Int(2254, 01, 01)), // SAM, Myochin, Kiku-Ichimonji
        (FunEquipSet.Group.FullSet(378, 1),                                CharacterWeapon.Int(2301, 01, 1), CharacterWeapon.Int(2351, 01, 01)), // RDM, Duelist, Murgleis
        (FunEquipSet.Group.FullSet(655, 1),                                CharacterWeapon.Int(2401, 01, 2), CharacterWeapon.Empty),             // BLU, Mirage, Predatrice
        (new FunEquipSet.Group(592, 3, 380, 4, 380, 4, 380, 4, 380, 4),    CharacterWeapon.Int(2501, 14, 1), CharacterWeapon.Empty),             // GNB, Outsider's, High Steel Gunblade
        (FunEquipSet.Group.FullSet(204, 4),                                CharacterWeapon.Int(2601, 13, 1), CharacterWeapon.Int(2651, 13, 01)), // DNC, Softstepper, High Steel Chakrams
        (new FunEquipSet.Group(206, 7, 303, 3, 23, 109, 303, 3, 262, 7),   CharacterWeapon.Int(2802, 13, 1), CharacterWeapon.Empty),             // RPR, Muzhik, Deepgold War Scythe
        (new FunEquipSet.Group(20, 46, 289, 6, 342, 3, 120, 9, 342, 3),    CharacterWeapon.Int(2702, 08, 1), CharacterWeapon.Empty),             // SGE, Bookwyrm, Stonegold Milpreves
    };

    private static readonly (FunEquipSet.Group, CharacterWeapon, CharacterWeapon)[] _80Artifact =
    {
        (FunEquipSet.Group.FullSet(000, 0),                                CharacterWeapon.Empty,             CharacterWeapon.Empty),              // ADV, Nothing
        (FunEquipSet.Group.FullSet(527, 1),                                CharacterWeapon.Int(0201, 116, 1), CharacterWeapon.Int(0110, 01, 001)), // GLA, Chevalier, Sequence, Srivatsa
        (new FunEquipSet.Group(528, 1, 528, 1, 8812, 1, 528, 1, 528, 1),   CharacterWeapon.Int(1601, 003, 1), CharacterWeapon.Int(1651, 03, 001)), // PGL, Bhikku, Godhands
        (FunEquipSet.Group.FullSet(529, 1),                                CharacterWeapon.Int(0401, 090, 1), CharacterWeapon.Empty),              // MRD, Boii, Chango
        (FunEquipSet.Group.FullSet(530, 1),                                CharacterWeapon.Int(0501, 082, 1), CharacterWeapon.Empty),              // LNC, Pteroslaver, Trishula
        (FunEquipSet.Group.FullSet(531, 1),                                CharacterWeapon.Int(0623, 001, 1), CharacterWeapon.Int(0698, 78, 001)), // ARC, Fili, Fail-Not
        (FunEquipSet.Group.FullSet(532, 1),                                CharacterWeapon.Int(0817, 001, 1), CharacterWeapon.Empty),              // CNJ, Ebers, Tishtrya
        (FunEquipSet.Group.FullSet(533, 1),                                CharacterWeapon.Int(1016, 001, 1), CharacterWeapon.Empty),              // THM, Wicce, Khatvanga
        (FunEquipSet.Group.FullSet(544, 1),                                CharacterWeapon.Int(5004, 003, 1), CharacterWeapon.Int(5041, 01, 010)), // CRP, Millfiend
        (FunEquipSet.Group.FullSet(545, 1),                                CharacterWeapon.Int(5101, 005, 1), CharacterWeapon.Int(5141, 01, 005)), // BSM, Forgefiend
        (FunEquipSet.Group.FullSet(546, 1),                                CharacterWeapon.Int(5201, 007, 1), CharacterWeapon.Int(5241, 01, 008)), // ARM, Hammerfiend
        (FunEquipSet.Group.FullSet(547, 1),                                CharacterWeapon.Int(5301, 006, 1), CharacterWeapon.Int(5341, 01, 001)), // GSM, Gemfiend
        (FunEquipSet.Group.FullSet(548, 1),                                CharacterWeapon.Int(5401, 007, 1), CharacterWeapon.Int(5441, 01, 007)), // LTW, Hidefiend
        (FunEquipSet.Group.FullSet(549, 1),                                CharacterWeapon.Int(5501, 005, 1), CharacterWeapon.Int(5571, 01, 001)), // WVR, Boltfiend
        (FunEquipSet.Group.FullSet(550, 1),                                CharacterWeapon.Int(5603, 004, 1), CharacterWeapon.Int(5641, 01, 011)), // ALC, Cauldronfiend
        (FunEquipSet.Group.FullSet(551, 1),                                CharacterWeapon.Int(5701, 008, 1), CharacterWeapon.Int(5741, 01, 008)), // CUL, Galleyfiend
        (FunEquipSet.Group.FullSet(552, 1),                                CharacterWeapon.Int(7002, 002, 1), CharacterWeapon.Int(7051, 01, 008)), // MIN, Minefiend
        (FunEquipSet.Group.FullSet(553, 1),                                CharacterWeapon.Int(7101, 008, 1), CharacterWeapon.Int(7151, 01, 008)), // BTN, Fieldfiend
        (FunEquipSet.Group.FullSet(554, 1),                                CharacterWeapon.Int(7201, 007, 1), CharacterWeapon.Int(7255, 01, 001)), // FSH, Tacklefiend
        (FunEquipSet.Group.FullSet(527, 1),                                CharacterWeapon.Int(0201, 116, 1), CharacterWeapon.Int(0110, 01, 001)), // PLD, Chevalier, Sequence, Srivatsa
        (new FunEquipSet.Group(528, 1, 528, 1, 8812, 1, 528, 1, 528, 1),   CharacterWeapon.Int(1601, 003, 1), CharacterWeapon.Int(1651, 03, 001)), // MNK, Bhikku, Godhands
        (FunEquipSet.Group.FullSet(529, 1),                                CharacterWeapon.Int(0401, 090, 1), CharacterWeapon.Empty),              // WAR, Boii, Chango
        (FunEquipSet.Group.FullSet(530, 1),                                CharacterWeapon.Int(0501, 082, 1), CharacterWeapon.Empty),              // DRG, Pteroslaver, Trishula
        (FunEquipSet.Group.FullSet(531, 1),                                CharacterWeapon.Int(0623, 001, 1), CharacterWeapon.Int(0698, 78, 001)), // BRD, Fili, Fail-Not
        (FunEquipSet.Group.FullSet(532, 1),                                CharacterWeapon.Int(0817, 001, 1), CharacterWeapon.Empty),              // WHM, Ebers, Tishtrya
        (FunEquipSet.Group.FullSet(533, 1),                                CharacterWeapon.Int(1016, 001, 1), CharacterWeapon.Empty),              // BLM, Wicce, Khatvanga
        (FunEquipSet.Group.FullSet(534, 1),                                CharacterWeapon.Int(1708, 002, 1), CharacterWeapon.Empty),              // ACN, Beckoner, Meteorologica
        (FunEquipSet.Group.FullSet(534, 1),                                CharacterWeapon.Int(1708, 002, 1), CharacterWeapon.Empty),              // SMN, Beckoner, Meteorologica
        (FunEquipSet.Group.FullSet(535, 1),                                CharacterWeapon.Int(1736, 001, 1), CharacterWeapon.Empty),              // SCH, Arbatel, Physica
        (FunEquipSet.Group.FullSet(536, 1),                                CharacterWeapon.Int(1801, 083, 1), CharacterWeapon.Int(1851, 83, 001)), // ROG, Hattori, Heishi Shorinken
        (FunEquipSet.Group.FullSet(536, 1),                                CharacterWeapon.Int(1801, 083, 1), CharacterWeapon.Int(1851, 83, 001)), // NIN, Hattori, Heishi Shorinken
        (FunEquipSet.Group.FullSet(538, 1),                                CharacterWeapon.Int(2001, 060, 1), CharacterWeapon.Int(2099, 01, 001)), // MCH, Gunslinger, Fomalhaut
        (FunEquipSet.Group.FullSet(537, 1),                                CharacterWeapon.Int(1501, 074, 1), CharacterWeapon.Empty),              // DRK, Bale, Shadowbringer
        (FunEquipSet.Group.FullSet(539, 1),                                CharacterWeapon.Int(2117, 001, 1), CharacterWeapon.Int(2199, 01, 118)), // AST, Soothsayer, Procyon
        (FunEquipSet.Group.FullSet(540, 1),                                CharacterWeapon.Int(2201, 043, 1), CharacterWeapon.Int(2251, 61, 001)), // SAM, Kasuga, Dojikiri Yasutsuna
        (FunEquipSet.Group.FullSet(541, 1),                                CharacterWeapon.Int(2301, 055, 1), CharacterWeapon.Int(2351, 38, 001)), // RDM, Estoqueur, Aeneas
        (FunEquipSet.Group.FullSet(811, 1),                                CharacterWeapon.Int(2401, 005, 1), CharacterWeapon.Empty),              // BLU, Phantasmal, Blue-eyes
        (FunEquipSet.Group.FullSet(542, 1),                                CharacterWeapon.Int(2505, 001, 1), CharacterWeapon.Empty),              // GNB, Bodyguard, Lion Heart
        (FunEquipSet.Group.FullSet(543, 1),                                CharacterWeapon.Int(2601, 001, 1), CharacterWeapon.Int(2651, 01, 001)), // DNC, Dancer, Krishna
        (new FunEquipSet.Group(206, 7, 303, 3, 23, 109, 303, 3, 262, 7),   CharacterWeapon.Int(2802, 013, 1), CharacterWeapon.Empty),              // RPR, Harvester's, Demon Slicer
        (new FunEquipSet.Group(20, 46, 289, 6, 342, 3, 120, 9, 342, 3),    CharacterWeapon.Int(2702, 008, 1), CharacterWeapon.Empty),              // SGE, Therapeute's, Horkos
    };

    private static readonly (FunEquipSet.Group, CharacterWeapon, CharacterWeapon)[] _90Artifact = 
    {
        (FunEquipSet.Group.FullSet(000, 0),                                CharacterWeapon.Empty,             CharacterWeapon.Empty),               // ADV, Nothing
        (FunEquipSet.Group.FullSet(678, 1),                                CharacterWeapon.Int(0201, 134, 1), CharacterWeapon.Int(0101, 100, 001)), // GLA, Reverence, Lightbringer, Hero's Shield
        (new FunEquipSet.Group(679, 1, 679, 1, 8816, 1, 679, 1, 679, 1),   CharacterWeapon.Int(1601, 007, 1), CharacterWeapon.Int(1651, 007, 001)), // PGL, Anchorite, Burning Fists
        (FunEquipSet.Group.FullSet(680, 1),                                CharacterWeapon.Int(0401, 111, 1), CharacterWeapon.Empty),               // MRD, Pummeler, Gigantaxe
        (FunEquipSet.Group.FullSet(681, 1),                                CharacterWeapon.Int(0501, 105, 1), CharacterWeapon.Empty),               // LNC, Tiamat, Abel's Lance
        (FunEquipSet.Group.FullSet(682, 1),                                CharacterWeapon.Int(0629, 001, 1), CharacterWeapon.Int(0698, 102, 001)), // ARC, Brioso, Perseus's Bow
        (FunEquipSet.Group.FullSet(683, 1),                                CharacterWeapon.Int(0801, 084, 1), CharacterWeapon.Empty),               // CNJ, Theophany, Xoanon
        (FunEquipSet.Group.FullSet(684, 1),                                CharacterWeapon.Int(1025, 001, 1), CharacterWeapon.Empty),               // THM, Spaekona, Asura's Rod
        (FunEquipSet.Group.FullSet(697, 1),                                CharacterWeapon.Int(5004, 005, 1), CharacterWeapon.Int(5041, 001, 010)), // CRP, Millsoph
        (FunEquipSet.Group.FullSet(698, 1),                                CharacterWeapon.Int(5101, 007, 1), CharacterWeapon.Int(5141, 001, 005)), // BSM, Forgesoph
        (FunEquipSet.Group.FullSet(699, 1),                                CharacterWeapon.Int(5201, 009, 1), CharacterWeapon.Int(5241, 001, 008)), // ARM, Hammersoph
        (FunEquipSet.Group.FullSet(700, 1),                                CharacterWeapon.Int(5301, 008, 1), CharacterWeapon.Int(5341, 001, 001)), // GSM, Gemsoph
        (FunEquipSet.Group.FullSet(701, 1),                                CharacterWeapon.Int(5401, 009, 1), CharacterWeapon.Int(5441, 001, 007)), // LTW, Hidesoph
        (FunEquipSet.Group.FullSet(702, 1),                                CharacterWeapon.Int(5501, 007, 1), CharacterWeapon.Int(5571, 001, 001)), // WVR, Boltsoph
        (FunEquipSet.Group.FullSet(703, 1),                                CharacterWeapon.Int(5603, 006, 1), CharacterWeapon.Int(5641, 001, 011)), // ALC, Cauldronsoph
        (FunEquipSet.Group.FullSet(704, 1),                                CharacterWeapon.Int(5701, 010, 1), CharacterWeapon.Int(5741, 001, 008)), // CUL, Galleysoph
        (FunEquipSet.Group.FullSet(705, 1),                                CharacterWeapon.Int(7002, 009, 1), CharacterWeapon.Int(7051, 001, 008)), // MIN, Minesoph
        (FunEquipSet.Group.FullSet(706, 1),                                CharacterWeapon.Int(7101, 008, 1), CharacterWeapon.Int(7151, 001, 008)), // BTN, Fieldsoph
        (FunEquipSet.Group.FullSet(707, 1),                                CharacterWeapon.Int(7201, 009, 1), CharacterWeapon.Int(7255, 001, 001)), // FSH, Tacklesoph
        (FunEquipSet.Group.FullSet(678, 1),                                CharacterWeapon.Int(0201, 134, 1), CharacterWeapon.Int(0101, 100, 001)), // PLD, Reverence, Lightbringer, Hero's Shield
        (new FunEquipSet.Group(679, 1, 679, 1, 8816, 1, 679, 1, 679, 1),   CharacterWeapon.Int(1601, 007, 1), CharacterWeapon.Int(1651, 007, 001)), // MNK, Anchorite, Burning Fists
        (FunEquipSet.Group.FullSet(680, 1),                                CharacterWeapon.Int(0401, 111, 1), CharacterWeapon.Empty),               // WAR, Pummeler, Gigantaxe
        (FunEquipSet.Group.FullSet(681, 1),                                CharacterWeapon.Int(0501, 105, 1), CharacterWeapon.Empty),               // DRG, Tiamat, Abel's Lance
        (FunEquipSet.Group.FullSet(682, 1),                                CharacterWeapon.Int(0629, 001, 1), CharacterWeapon.Int(0698, 102, 001)), // BRD, Brioso, Perseus's Bow
        (FunEquipSet.Group.FullSet(683, 1),                                CharacterWeapon.Int(0801, 084, 1), CharacterWeapon.Empty),               // WHM, Theophany, Xoanon
        (FunEquipSet.Group.FullSet(684, 1),                                CharacterWeapon.Int(1025, 001, 1), CharacterWeapon.Empty),               // BLM, Spaekona, Asura's Rod
        (FunEquipSet.Group.FullSet(685, 1),                                CharacterWeapon.Int(1701, 096, 1), CharacterWeapon.Empty),               // ACN, Convoker, Abraxas
        (FunEquipSet.Group.FullSet(685, 1),                                CharacterWeapon.Int(1701, 096, 1), CharacterWeapon.Empty),               // SMN, Convoker, Abraxas
        (FunEquipSet.Group.FullSet(686, 1),                                CharacterWeapon.Int(1701, 099, 1), CharacterWeapon.Empty),               // SCH, Academic, Epeolatry
        (FunEquipSet.Group.FullSet(687, 1),                                CharacterWeapon.Int(1801, 104, 1), CharacterWeapon.Int(1851, 104, 001)), // ROG, Hachiya, Mutsunokami
        (FunEquipSet.Group.FullSet(687, 1),                                CharacterWeapon.Int(1801, 104, 1), CharacterWeapon.Int(1851, 104, 001)), // NIN, Hachiya, Mutsunokami
        (FunEquipSet.Group.FullSet(689, 1),                                CharacterWeapon.Int(2001, 080, 1), CharacterWeapon.Int(2099, 001, 001)), // MCH, Pioneer, Ataktos
        (FunEquipSet.Group.FullSet(688, 1),                                CharacterWeapon.Int(1501, 103, 1), CharacterWeapon.Empty),               // DRK, Ignominy, Chaosbringer
        (FunEquipSet.Group.FullSet(690, 1),                                CharacterWeapon.Int(2101, 082, 1), CharacterWeapon.Int(2199, 001, 149)), // AST, Astronomia, Diana
        (FunEquipSet.Group.FullSet(691, 1),                                CharacterWeapon.Int(2201, 064, 1), CharacterWeapon.Int(2251, 082, 001)), // SAM, Saotome, Murasame
        (FunEquipSet.Group.FullSet(692, 1),                                CharacterWeapon.Int(2305, 004, 1), CharacterWeapon.Int(2351, 064, 001)), // RDM, Atrophy, Wild Rose
        (FunEquipSet.Group.FullSet(811, 1),                                CharacterWeapon.Int(2401, 005, 1), CharacterWeapon.Empty),               // BLU, Phantasmal, Blue-eyes
        (FunEquipSet.Group.FullSet(693, 1),                                CharacterWeapon.Int(2501, 044, 1), CharacterWeapon.Empty),               // GNB, Allegiance, Hyperion
        (FunEquipSet.Group.FullSet(694, 1),                                CharacterWeapon.Int(2607, 001, 1), CharacterWeapon.Int(2657, 001, 001)), // DNC, Etoile, Terpsichore
        (FunEquipSet.Group.FullSet(695, 1),                                CharacterWeapon.Int(2801, 001, 1), CharacterWeapon.Empty),               // RPR, Reaper, Death Sickle
        (FunEquipSet.Group.FullSet(696, 1),                                CharacterWeapon.Int(2701, 006, 1), CharacterWeapon.Empty),               // SGE, Didact, Hagneia
    };
    // @formatter:on

    private (FunEquipSet.Group, CharacterWeapon, CharacterWeapon)? GetGroup(byte level, byte job, Race race, Gender gender, Random rng)
    {
        const int weight50      = 200;
        const int weight60      = 500;
        const int weight70      = 700;
        const int weight80      = 800;
        const int weight90      = 900;
        const int weight100     = 1000;

        if (job >= StarterWeapons.Length)
            return null;

        var maxWeight = level switch
        {
            < 50 => weight50,
            < 60 => weight60,
            < 70 => weight70,
            < 80 => weight80,
            < 90 => weight90,
            _    => weight100,
        };

        var weight = rng.Next(0, maxWeight + 1);
        if (weight < weight50)
        {
            var (main, off) = StarterWeapons[job];
            if (!Starter.TryGetValue((gender, race), out var group))
                return null;

            return (group, main, off);
        }

        var list = weight switch
        {
            < weight60 => _50Artifact,
            < weight70 => _60Artifact,
            < weight80 => _70Artifact,
            < weight90 => _80Artifact,
            _          => _90Artifact,
        };

        Glamourer.Log.Verbose($"Chose weight {weight}/{maxWeight} for World set [Character: {level} {job} {race} {gender}].");
        return list[job];
    }


    private static unsafe (byte, byte, Race, Gender) GetData(Actor actor)
    {
        var customize = actor.Customize;
        return (actor.AsCharacter->CharacterData.Level, actor.Job, customize->Race, customize->Gender);
    }

    public void Apply(Actor actor, Random rng, Span<CharacterArmor> armor)
    {
        var (level, job, race, gender) = GetData(actor);
        Apply(level, job, race, gender, rng, armor);
    }

    private void Apply(byte level, byte job, Race race, Gender gender, Random rng, Span<CharacterArmor> armor)
    {
        var opt = GetGroup(level, job, race, gender, rng);
        if (opt == null)
            return;

        armor[0] = opt.Value.Item1.Head;
        armor[1] = opt.Value.Item1.Body;
        armor[2] = opt.Value.Item1.Hands;
        armor[3] = opt.Value.Item1.Legs;
        armor[4] = opt.Value.Item1.Feet;
    }

    public void Apply(Actor actor, Random rng, ref CharacterArmor armor, EquipSlot slot)
    {
        var (level, job, race, gender) = GetData(actor);
        Apply(level, job, race, gender, rng, ref armor, slot);
    }

    private void Apply(byte level, byte job, Race race, Gender gender, Random rng, ref CharacterArmor armor, EquipSlot slot)
    {
        var opt = GetGroup(level, job, race, gender, rng);
        if (opt == null)
            return;

        armor = slot switch
        {
            EquipSlot.Head  => opt.Value.Item1.Head,
            EquipSlot.Body  => opt.Value.Item1.Body,
            EquipSlot.Hands => opt.Value.Item1.Hands,
            EquipSlot.Legs  => opt.Value.Item1.Legs,
            EquipSlot.Feet  => opt.Value.Item1.Feet,
            _               => armor,
        };
    }

    public void Apply(Actor actor, Random rng, ref CharacterWeapon weapon, EquipSlot slot)
    {
        var (level, job, race, gender) = GetData(actor);
        Apply(level, job, race, gender, rng, ref weapon, slot);
    }

    private void Apply(byte level, byte job, Race race, Gender gender, Random rng, ref CharacterWeapon weapon, EquipSlot slot)
    {
        var opt = GetGroup(level, job, race, gender, rng);
        if (opt == null)
            return;

        weapon = slot switch
        {
            EquipSlot.MainHand => opt.Value.Item2,
            EquipSlot.BothHand => opt.Value.Item2,
            EquipSlot.OffHand  => opt.Value.Item3,
            _                  => weapon,
        };
    }
}
