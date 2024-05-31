using Penumbra.GameData.Enums;

namespace Glamourer.Services;

public class CodeService
{
    private readonly Configuration _config;
    private readonly SHA256        _hasher = SHA256.Create();

    [Flags]
    public enum CodeFlag : ulong
    {
        Clown        = 0x000001,
        Emperor      = 0x000002,
        Individual   = 0x000004,
        Dwarf        = 0x000008,
        Giant        = 0x000010,
        OopsHyur     = 0x000020,
        OopsElezen   = 0x000040,
        OopsLalafell = 0x000080,
        OopsMiqote   = 0x000100,
        OopsRoegadyn = 0x000200,
        OopsAuRa     = 0x000400,
        OopsHrothgar = 0x000800,
        OopsViera    = 0x001000,
        Artisan      = 0x002000,
        SixtyThree   = 0x004000,
        Shirts       = 0x008000,
        World        = 0x010000,
        Elephants    = 0x020000,
        Crown        = 0x040000,
        Dolphins     = 0x080000,
    }

    public static readonly CodeFlag AllHintCodes =
        Enum.GetValues<CodeFlag>().Where(f => GetData(f).Display).Aggregate((CodeFlag)0, (f1, f2) => f1 | f2);

    public const CodeFlag DyeCodes  = CodeFlag.Clown | CodeFlag.World | CodeFlag.Elephants | CodeFlag.Dolphins;
    public const CodeFlag GearCodes = CodeFlag.Emperor | CodeFlag.World | CodeFlag.Elephants | CodeFlag.Dolphins;

    public const CodeFlag RaceCodes = CodeFlag.OopsHyur
      | CodeFlag.OopsElezen
      | CodeFlag.OopsLalafell
      | CodeFlag.OopsMiqote
      | CodeFlag.OopsRoegadyn
      | CodeFlag.OopsAuRa
      | CodeFlag.OopsHrothgar;

    public const CodeFlag SizeCodes = CodeFlag.Dwarf | CodeFlag.Giant;

    private CodeFlag _enabled;

    public bool Enabled(CodeFlag flag)
        => _enabled.HasFlag(flag);

    public bool AnyEnabled(CodeFlag flag)
        => (_enabled & flag) != 0;

    public CodeFlag Masked(CodeFlag mask)
        => _enabled & mask;

    public Race GetRace()
        => (_enabled & RaceCodes) switch
        {
            CodeFlag.OopsHyur     => Race.Hyur,
            CodeFlag.OopsElezen   => Race.Elezen,
            CodeFlag.OopsLalafell => Race.Lalafell,
            CodeFlag.OopsMiqote   => Race.Miqote,
            CodeFlag.OopsRoegadyn => Race.Roegadyn,
            CodeFlag.OopsAuRa     => Race.AuRa,
            CodeFlag.OopsHrothgar => Race.Hrothgar,
            CodeFlag.OopsViera    => Race.Viera,
            _                     => Race.Unknown,
        };

    public CodeService(Configuration config)
    {
        _config = config;
        Load();
    }

    private void Load()
    {
        var changes = false;
        for (var i = 0; i < _config.Codes.Count; ++i)
        {
            var enabled = CheckCode(_config.Codes[i].Code).Item1;
            if (enabled == null)
            {
                _config.Codes.RemoveAt(i--);
                changes = true;
            }
            else
            {
                var value = _config.Codes[i].Enabled;
                enabled(value);
            }
        }

        if (changes)
            _config.Save();
    }

    public bool AddCode(string name)
    {
        if (CheckCode(name).Item1 is null || _config.Codes.Any(p => p.Code == name))
            return false;

        _config.Codes.Add((name, false));
        _config.Save();
        return true;
    }

    public (Action<bool>?, CodeFlag) CheckCode(string name)
    {
        var flag = GetCode(name);
        if (flag == 0)
            return (null, 0);

        var badFlags = ~GetMutuallyExclusive(flag);
        return (v => _enabled = v ? (_enabled | flag) & badFlags : _enabled & ~flag, flag);
    }

    public CodeFlag GetCode(string name)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(name));
        var       sha    = (ReadOnlySpan<byte>)_hasher.ComputeHash(stream);

        foreach (var flag in Enum.GetValues<CodeFlag>())
        {
            if (sha.SequenceEqual(GetSha(flag)))
                return flag;
        }

        return 0;
    }

    /// <summary> Update all enabled states in the config. </summary>
    public void SaveState()
    {
        for (var i = 0; i < _config.Codes.Count; ++i)
        {
            var name = _config.Codes[i].Code;
            var flag = GetCode(name);
            if (flag == 0)
            {
                _config.Codes.RemoveAt(i--);
                continue;
            }

            _config.Codes[i] = (name, Enabled(flag));
        }

        _config.Save();
    }

    // @formatter:off
    private static CodeFlag GetMutuallyExclusive(CodeFlag flag)
        => flag switch
        {
            CodeFlag.Clown        => DyeCodes & ~CodeFlag.Clown,
            CodeFlag.Emperor      => GearCodes & ~CodeFlag.Emperor,
            CodeFlag.Individual   => 0,
            CodeFlag.Dwarf        => SizeCodes & ~CodeFlag.Dwarf,
            CodeFlag.Giant        => SizeCodes & ~CodeFlag.Giant,
            CodeFlag.OopsHyur     => RaceCodes & ~CodeFlag.OopsHyur,
            CodeFlag.OopsElezen   => RaceCodes & ~CodeFlag.OopsElezen,
            CodeFlag.OopsLalafell => RaceCodes & ~CodeFlag.OopsLalafell,
            CodeFlag.OopsMiqote   => RaceCodes & ~CodeFlag.OopsMiqote,
            CodeFlag.OopsRoegadyn => RaceCodes & ~CodeFlag.OopsRoegadyn,
            CodeFlag.OopsAuRa     => RaceCodes & ~CodeFlag.OopsAuRa,
            CodeFlag.OopsHrothgar => RaceCodes & ~CodeFlag.OopsHrothgar,
            CodeFlag.OopsViera    => RaceCodes & ~CodeFlag.OopsViera,
            CodeFlag.Artisan      => 0,
            CodeFlag.SixtyThree   => 0,
            CodeFlag.Shirts       => 0,
            CodeFlag.World        => (DyeCodes | GearCodes) & ~CodeFlag.World,
            CodeFlag.Elephants    => (DyeCodes | GearCodes) & ~CodeFlag.Elephants,
            CodeFlag.Crown        => 0,
            CodeFlag.Dolphins     => (DyeCodes | GearCodes) & ~CodeFlag.Dolphins,
            _                     => 0,
        };
    
    private static ReadOnlySpan<byte> GetSha(CodeFlag flag)
        => flag switch
        {
            CodeFlag.Clown        => [ 0xC4, 0xEE, 0x1D, 0x6F, 0xC5, 0x5D, 0x47, 0xBE, 0x78, 0x63, 0x66, 0x86, 0x81, 0x15, 0xEB, 0xFA, 0xF6, 0x4A, 0x90, 0xEA, 0xC0, 0xE4, 0xEE, 0x86, 0x69, 0x01, 0x8E, 0xDB, 0xCC, 0x69, 0xD1, 0xBD ],
            CodeFlag.Emperor      => [ 0xE2, 0x2D, 0x3E, 0x57, 0x16, 0x82, 0x65, 0x98, 0x7E, 0xE6, 0x8F, 0x45, 0x14, 0x7D, 0x65, 0x31, 0xE9, 0xD8, 0xDB, 0xEA, 0xDC, 0xBF, 0xEE, 0x2A, 0xBA, 0xD5, 0x69, 0x96, 0x78, 0x34, 0x3B, 0x57 ],
            CodeFlag.Individual   => [ 0x95, 0xA4, 0x71, 0xAC, 0xA3, 0xC2, 0x34, 0x94, 0xC1, 0x65, 0x07, 0xF3, 0x7F, 0x93, 0x57, 0xEE, 0xE3, 0x04, 0xC0, 0xE8, 0x1B, 0xA0, 0xE2, 0x08, 0x68, 0x02, 0x8D, 0xAD, 0x76, 0x03, 0x9B, 0xC5 ],
            CodeFlag.Dwarf        => [ 0x55, 0x97, 0xFE, 0xE9, 0x78, 0x64, 0xE8, 0x2F, 0xCD, 0x25, 0xD1, 0xAE, 0xDF, 0x35, 0xE6, 0xED, 0x03, 0x78, 0x54, 0x1D, 0x56, 0x22, 0x34, 0x75, 0x4B, 0x96, 0x6F, 0xBA, 0xAC, 0xEC, 0x00, 0x46 ],
            CodeFlag.Giant        => [ 0x6E, 0xBB, 0x91, 0x1D, 0x67, 0xE3, 0x00, 0x07, 0xA1, 0x0F, 0x2A, 0xF0, 0x26, 0x91, 0x38, 0x63, 0xD3, 0x52, 0x82, 0xF7, 0x5D, 0x93, 0xE8, 0x83, 0xB1, 0xF6, 0xB9, 0x69, 0x78, 0x20, 0xC4, 0xCE ],
            CodeFlag.OopsHyur     => [ 0x4C, 0x51, 0xE2, 0x38, 0xEF, 0xAD, 0x84, 0x0E, 0x4E, 0x11, 0x0F, 0x5E, 0xDE, 0x45, 0x41, 0x9F, 0x6A, 0xF6, 0x5F, 0x5B, 0xA8, 0x91, 0x64, 0x22, 0xEE, 0x62, 0x97, 0x3C, 0x78, 0x18, 0xCD, 0xAF ],
            CodeFlag.OopsElezen   => [ 0x3D, 0x5B, 0xA9, 0x62, 0xCE, 0xBE, 0x52, 0xF5, 0x94, 0x2A, 0xF9, 0xB7, 0xCF, 0xD9, 0x24, 0x2B, 0x38, 0xC7, 0x4F, 0x28, 0x97, 0x29, 0x1D, 0x01, 0x13, 0x53, 0x44, 0x11, 0x15, 0x6F, 0x9B, 0x56 ],
            CodeFlag.OopsLalafell => [ 0x85, 0x8D, 0x5B, 0xC2, 0x66, 0x53, 0x2E, 0xB9, 0xE9, 0x85, 0xE5, 0xF8, 0xD3, 0x75, 0x18, 0x7C, 0x58, 0x55, 0xD4, 0x8C, 0x8E, 0x5F, 0x58, 0x2E, 0xF3, 0xF1, 0xAE, 0xA8, 0xA0, 0x81, 0xC6, 0x0E ],
            CodeFlag.OopsMiqote   => [ 0x44, 0x73, 0x8C, 0x39, 0x5A, 0xF1, 0xDB, 0x5F, 0x62, 0xA1, 0x6E, 0x5F, 0xE6, 0x97, 0x9E, 0x90, 0xD7, 0x5C, 0x97, 0x67, 0xB6, 0xC7, 0x99, 0x61, 0x36, 0xCA, 0x34, 0x7E, 0xB9, 0xAC, 0xC3, 0x76 ],
            CodeFlag.OopsRoegadyn => [ 0xB7, 0x25, 0x73, 0xDB, 0xBE, 0xD0, 0x49, 0xFB, 0xFF, 0x9C, 0x32, 0x21, 0xB0, 0x8A, 0x2C, 0x0C, 0x77, 0x46, 0xD5, 0xCF, 0x0E, 0x63, 0x2F, 0x91, 0x85, 0x8B, 0x55, 0x5C, 0x4D, 0xD2, 0xB9, 0xB8 ],
            CodeFlag.OopsAuRa     => [ 0x69, 0x93, 0xAF, 0xE4, 0xB8, 0xEC, 0x5F, 0x40, 0xEB, 0x8A, 0x6F, 0xD1, 0x9B, 0xD9, 0x56, 0x0B, 0xEA, 0x64, 0x79, 0x9B, 0x54, 0xA1, 0x41, 0xED, 0xBC, 0x3E, 0x6E, 0x5C, 0xF1, 0x23, 0x60, 0xF8 ],
            CodeFlag.OopsHrothgar => [ 0x41, 0xEC, 0x65, 0x05, 0x8D, 0x20, 0x68, 0x5A, 0xB7, 0xEB, 0x92, 0x15, 0x43, 0xCF, 0x15, 0x05, 0x27, 0x51, 0xFE, 0x20, 0xC9, 0xB6, 0x2B, 0x84, 0xD9, 0x6A, 0x49, 0x5A, 0x5B, 0x7F, 0x2E, 0xE7 ],
            CodeFlag.OopsViera    => [ 0x16, 0xFF, 0x63, 0x85, 0x1C, 0xF5, 0x34, 0x33, 0x67, 0x8C, 0x46, 0x8E, 0x3E, 0xE3, 0xA6, 0x94, 0xF9, 0x74, 0x47, 0xAA, 0xC7, 0x29, 0x59, 0x1F, 0x6C, 0x6E, 0xF2, 0xF5, 0x87, 0x24, 0x9E, 0x2B ],
            CodeFlag.Artisan      => [ 0xDE, 0x01, 0x32, 0x1E, 0x7F, 0x22, 0x80, 0x3D, 0x76, 0xDF, 0x74, 0x0E, 0xEC, 0x33, 0xD3, 0xF4, 0x1A, 0x98, 0x9E, 0x9D, 0x22, 0x5C, 0xAC, 0x3B, 0xFE, 0x0B, 0xC2, 0x13, 0xB9, 0x91, 0x24, 0x61 ],
            CodeFlag.SixtyThree   => [ 0xA1, 0x65, 0x60, 0x99, 0xB0, 0x9F, 0xBF, 0xD7, 0x20, 0xC8, 0x29, 0xF6, 0x7B, 0x86, 0x27, 0xF5, 0xBE, 0xA9, 0x5B, 0xB0, 0x20, 0x5E, 0x57, 0x7B, 0xFF, 0xBC, 0x1E, 0x8C, 0x04, 0xF9, 0x35, 0xD3 ],
            CodeFlag.Shirts       => [ 0xD1, 0x35, 0xD7, 0x18, 0xBE, 0x45, 0x42, 0xBD, 0x88, 0x77, 0x7E, 0xC4, 0x41, 0x06, 0x34, 0x4D, 0x71, 0x3A, 0xC5, 0xCC, 0xA4, 0x1B, 0x7D, 0x3F, 0x3B, 0x86, 0x07, 0xCB, 0x63, 0xD7, 0xF9, 0xDB ],
            CodeFlag.World        => [ 0xFD, 0xA2, 0xD2, 0xBC, 0xD9, 0x8A, 0x7E, 0x2B, 0x52, 0xCB, 0x57, 0x6E, 0x3A, 0x2E, 0x30, 0xBA, 0x4E, 0xAE, 0x42, 0xEA, 0x5C, 0x57, 0xDF, 0x17, 0x37, 0x3C, 0xCE, 0x17, 0x42, 0x43, 0xAE, 0xD0 ],
            CodeFlag.Elephants    => [ 0x9F, 0x4C, 0xCF, 0x6D, 0xC4, 0x01, 0x31, 0x46, 0x02, 0x05, 0x31, 0xED, 0xED, 0xB2, 0x66, 0x29, 0x31, 0x09, 0x1E, 0xE7, 0x47, 0xDE, 0x7B, 0x03, 0xB0, 0x3C, 0x06, 0x76, 0x26, 0x91, 0xDF, 0xB2 ],
            CodeFlag.Crown        => [ 0x43, 0x8E, 0x34, 0x56, 0x24, 0xC9, 0xC6, 0xDE, 0x2A, 0x68, 0x3A, 0x5D, 0xF5, 0x8E, 0xCB, 0xEF, 0x0D, 0x4D, 0x5B, 0xDC, 0x23, 0xF9, 0xF9, 0xBD, 0xD9, 0x60, 0xAD, 0x53, 0xC5, 0xA0, 0x33, 0xC4 ],
            CodeFlag.Dolphins     => [ 0x64, 0xC6, 0x2E, 0x7C, 0x22, 0x3A, 0x42, 0xF5, 0xC3, 0x93, 0x4F, 0x70, 0x1F, 0xFD, 0xFA, 0x3C, 0x98, 0xD2, 0x7C, 0xD8, 0x88, 0xA7, 0x3D, 0x1D, 0x0D, 0xD6, 0x70, 0x15, 0x28, 0x2E, 0x79, 0xE7 ],
            _                     => [],
        };

    public static (bool Display, int CapitalCount, string Punctuation, string Hint, string Effect) GetData(CodeFlag flag)
    => flag switch
    {
        CodeFlag.Clown        => (true,  3, ",.",    "A punchline uttered by Rorschach.",                                                                      "Randomizes dyes for every player."),
        CodeFlag.Emperor      => (true,  1, ".",     "A truth about clothes that only a child will speak.",                                                    "Randomizes clothing for every player."),
        CodeFlag.Individual   => (true,  2, "'!'!",  "Something an unwilling prophet tries to convince his followers of.",                                     "Randomizes customizations for every player."),
        CodeFlag.Dwarf        => (true,  1, "!",     "A centuries old metaphor about humility and the progress of science.",                                   "Sets the player character to minimum height and all other players to maximum height."),
        CodeFlag.Giant        => (true,  2, "!",     "A Swift renaming of one of the most famous literary openings of all time.",                              "Sets the player character to maximum height and all other players to minimum height."),
        CodeFlag.OopsHyur     => (true,  1, "','.",  "An alkaline quote attributed to Marilyn Monroe.",                                                        "Turns all players to Hyur."),
        CodeFlag.OopsElezen   => (true,  1, ".",     "A line from a Futurama song about the far future.",                                                      "Turns all players to Elezen."),
        CodeFlag.OopsLalafell => (true,  2, ",!",    "The name of a discontinued plugin.",                                                                     "Turns all players to Lalafell."),
        CodeFlag.OopsMiqote   => (true,  3, ".",     "A Sandman story.",                                                                                       "Turns all players to Miqo'te."),
        CodeFlag.OopsRoegadyn => (true,  2, "!",     "A line from a Steven Universe song about his desires.",                                                  "Turns all players to Roegadyn."),
        CodeFlag.OopsAuRa     => (true,  1, "',.",   "Something a plumber hates to hear, made to something a scaly hates to hear and initial Au Ra designs.",  "Turns all players to Au Ra."),
        CodeFlag.OopsHrothgar => (true,  1, "',...", "A meme about the attractiveness of anthropomorphic animals.",                                            "Turns all players to Hrothgar."),
        CodeFlag.OopsViera    => (true,  2, "!'!",   "A panicked exclamation about bunny arithmetics.",                                                        "Turns all players to Viera."),
        CodeFlag.SixtyThree   => (true,  2, "",      "The title of a famous LGBTQ-related french play and movie.",                                             "Inverts the gender of every player."),
        CodeFlag.Shirts       => (true,  2, "-.",    "A pre-internet meme about disappointing rewards for an adventure, adapted to this specific cheat code.", "Highlights all items in the Unlocks tab as if they were unlocked."),
        CodeFlag.World        => (true,  1, ",.",    "A quote about being more important than other people.",                                                  "Sets every player except the player character themselves to job-appropriate gear."),
        CodeFlag.Elephants    => (true,  1, "!",     "Appropriate lyrics that can also be found in Glamourer's changelogs.",                                   "Sets every player to the elephant costume in varying shades of pink."),
        CodeFlag.Crown        => (true,  1, ".",     "A famous Shakespearean line.",                                                                           "Sets every player with a mentor symbol enabled to the clown's hat."),
        CodeFlag.Dolphins     => (true,  5, ",",     "The farewell of the second smartest species on Earth.",                                                  "Sets every player to a Namazu hat with different costume bodies."),
        CodeFlag.Artisan      => (false, 3, ",,!",   string.Empty,                                                                                             "Enable a debugging mode for the UI. Not really useful."),
        _                     => (false, 0, string.Empty, string.Empty, string.Empty),
    };
}
