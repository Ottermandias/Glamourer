using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Glamourer.Customization;
using Glamourer.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Saves;

public partial class Design
{
    public const int CurrentVersion = 1;

    public FileInfo Identifier  { get; private set; } = new(string.Empty);
    public string   Name        { get; private set; } = "New Design";
    public string   Description { get; private set; } = string.Empty;

    public DateTimeOffset CreationDate   { get; private init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastUpdateDate { get; private set; }  = DateTimeOffset.UtcNow;

    private DesignFlagsV1 _flags;

    public bool VisorState
    {
        get => _flags.HasFlag(DesignFlagsV1.VisorState);
        private set => _flags = value ? _flags | DesignFlagsV1.VisorState : _flags & ~DesignFlagsV1.VisorState;
    }

    public bool VisorApply
    {
        get => _flags.HasFlag(DesignFlagsV1.VisorApply);
        private set => _flags = value ? _flags | DesignFlagsV1.VisorApply : _flags & ~DesignFlagsV1.VisorApply;
    }

    public bool WeaponStateShown
    {
        get => _flags.HasFlag(DesignFlagsV1.WeaponStateShown);
        private set => _flags = value ? _flags | DesignFlagsV1.WeaponStateShown : _flags & ~DesignFlagsV1.WeaponStateShown;
    }

    public bool WeaponStateApply
    {
        get => _flags.HasFlag(DesignFlagsV1.WeaponStateApply);
        private set => _flags = value ? _flags | DesignFlagsV1.WeaponStateApply : _flags & ~DesignFlagsV1.WeaponStateApply;
    }

    public bool WetnessState
    {
        get => _flags.HasFlag(DesignFlagsV1.WetnessState);
        private set => _flags = value ? _flags | DesignFlagsV1.WetnessState : _flags & ~DesignFlagsV1.WetnessState;
    }

    public bool WetnessApply
    {
        get => _flags.HasFlag(DesignFlagsV1.WetnessApply);
        private set => _flags = value ? _flags | DesignFlagsV1.WetnessApply : _flags & ~DesignFlagsV1.WetnessApply;
    }

    public bool ReadOnly
    {
        get => _flags.HasFlag(DesignFlagsV1.ReadOnly);
        private set => _flags = value ? _flags | DesignFlagsV1.ReadOnly : _flags & ~DesignFlagsV1.ReadOnly;
    }

    private static bool FromDesignable(string identifier, string name, IDesignable data, [NotNullWhen(true)] out Design? design,
        bool doWeapons = true, bool doFlags = true, bool doEquipment = true, bool doCustomize = true)
    {
        if (!data.Valid)
        {
            design = null;
            return false;
        }

        design = new Design
        {
            Identifier       = new FileInfo(identifier),
            Name             = name,
            Description      = string.Empty,
            CreationDate     = DateTimeOffset.UtcNow,
            LastUpdateDate   = DateTimeOffset.UtcNow,
            ReadOnly         = false,
            VisorApply       = doFlags,
            WeaponStateApply = doFlags,
            WetnessApply     = doFlags,
            VisorState       = data.VisorEnabled,
            WeaponStateShown = data.WeaponEnabled,
            WetnessState     = data.IsWet,
        };

        if (doEquipment)
        {
            var equipment = data.Equip;
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                var s = design[slot];
                var e = equipment[slot];
                s.StainId    = e.Stain;
                s.ApplyStain = true;
                s.ItemId     = Glamourer.Identifier.Identify(e.Set, e.Variant, slot).FirstOrDefault()?.RowId ?? 0;
                s.ApplyItem  = s.ItemId != 0;
            }
        }

        if (doWeapons)
        {
            var m = design.MainHand;
            var d = data.MainHand;

            m.StainId    = d.Stain;
            m.ApplyStain = true;
            m.ItemId     = Glamourer.Identifier.Identify(d.Set, d.Type, d.Variant, EquipSlot.MainHand).FirstOrDefault()?.RowId ?? 0;
            m.ApplyItem  = m.ItemId != 0;

            var o = design.OffHand;
            d            = data.OffHand;
            o.StainId    = d.Stain;
            o.ApplyStain = true;
            o.ItemId     = Glamourer.Identifier.Identify(d.Set, d.Type, d.Variant, EquipSlot.MainHand).FirstOrDefault()?.RowId ?? 0;
            o.ApplyItem  = o.ItemId != 0;
        }

        if (doCustomize)
        {
            var customize = data.Customize;
            design.CustomizeFlags = Glamourer.Customization.GetList(customize.Clan, customize.Gender).SettingAvailable
              | CustomizeFlag.Gender
              | CustomizeFlag.Race
              | CustomizeFlag.Clan;
            foreach (var c in Enum.GetValues<CustomizeIndex>())
            {
                if (!design.CustomizeFlags.HasFlag(c.ToFlag()))
                    continue;

                var choice = design[c];
                choice.Value = customize[c];
            }
        }


        return true;
    }

    public void Save()
    {
        try
        {
            using var file = File.Open(Identifier.FullName, File.Exists(Identifier.FullName) ? FileMode.Truncate : FileMode.CreateNew);
            WriteJson(file);
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not save design {Identifier.Name}:\n{ex}");
        }
    }

    public void WriteJson(Stream s, Formatting formatting = Formatting.Indented)
    {
        var obj = new JObject();
        obj["Version"]              = CurrentVersion;
        obj[nameof(Name)]           = Name;
        obj[nameof(Description)]    = Description;
        obj[nameof(CreationDate)]   = CreationDate.ToUnixTimeSeconds();
        obj[nameof(LastUpdateDate)] = LastUpdateDate.ToUnixTimeSeconds();
        obj[nameof(ReadOnly)]       = ReadOnly;
        WriteEquipment(obj);
        WriteCustomization(obj);
        WriteFlags(obj);

        using var t = new StreamWriter(s);
        using var j = new JsonTextWriter(t) { Formatting = formatting };
        obj.WriteTo(j);
    }

    private void WriteFlags(JObject obj)
    {
        obj[nameof(VisorState)]       = VisorState;
        obj[nameof(VisorApply)]       = VisorApply;
        obj[nameof(WeaponStateShown)] = WeaponStateShown;
        obj[nameof(WeaponStateApply)] = WeaponStateApply;
        obj[nameof(WetnessState)]     = WetnessState;
        obj[nameof(WetnessApply)]     = WetnessApply;
    }

    public static bool Load(string fileName, [NotNullWhen(true)] out Design? design)
    {
        design = null;
        if (!File.Exists(fileName))
        {
            Glamourer.Log.Error($"Could not load design {fileName}:\nFile does not exist.");
            return false;
        }

        try
        {
            var data = File.ReadAllText(fileName);
            var obj  = JObject.Parse(data);

            return obj["Version"]?.Value<int>() switch
            {
                null => NoVersion(fileName),
                1    => LoadV1(fileName, obj, out design),
                _    => UnknownVersion(fileName, obj["Version"]!.Value<int>()),
            };
        }
        catch (Exception e)
        {
            Glamourer.Log.Error($"Could not load design {fileName}:\n{e}");
        }

        return false;
    }

    private static bool NoVersion(string fileName)
    {
        Glamourer.Log.Error($"Could not load design {fileName}:\nNo version available.");
        return false;
    }

    private static bool UnknownVersion(string fileName, int version)
    {
        Glamourer.Log.Error($"Could not load design {fileName}:\nThe version {version} can not be handled.");
        return false;
    }

    private static bool LoadV1(string fileName, JObject obj, [NotNullWhen(true)] out Design? design)
    {
        design = new Design
        {
            Identifier       = new FileInfo(fileName),
            Name             = obj[nameof(Name)]?.Value<string>() ?? "New Design",
            Description      = obj[nameof(Description)]?.Value<string>() ?? string.Empty,
            CreationDate     = GetDateTime(obj[nameof(CreationDate)]?.Value<long>()),
            LastUpdateDate   = GetDateTime(obj[nameof(LastUpdateDate)]?.Value<long>()),
            ReadOnly         = obj[nameof(ReadOnly)]?.Value<bool>() ?? false,
            VisorState       = obj[nameof(VisorState)]?.Value<bool>() ?? false,
            VisorApply       = obj[nameof(VisorApply)]?.Value<bool>() ?? false,
            WeaponStateShown = obj[nameof(WeaponStateShown)]?.Value<bool>() ?? false,
            WeaponStateApply = obj[nameof(WeaponStateApply)]?.Value<bool>() ?? false,
            WetnessState     = obj[nameof(WetnessState)]?.Value<bool>() ?? false,
            WetnessApply     = obj[nameof(WetnessApply)]?.Value<bool>() ?? false,
        };

        var equipment = obj[nameof(Equipment)];
        if (equipment == null)
        {
            design.EquipmentFlags = 0;
            design.StainFlags     = 0;
            design._equipmentData = default;
        }
        else
        {
            foreach (var slot in design.Equipment)
            {
                var s = equipment[SlotName[slot.Index]];
                if (s == null)
                {
                    slot.ItemId     = 0;
                    slot.ApplyItem  = false;
                    slot.ApplyStain = false;
                    slot.StainId    = 0;
                }
                else
                {
                    slot.ItemId     = s[nameof(Slot.ItemId)]?.Value<uint>() ?? 0u;
                    slot.ApplyItem  = obj[nameof(Slot.ApplyItem)]?.Value<bool>() ?? false;
                    slot.StainId    = new StainId(s[nameof(Slot.StainId)]?.Value<byte>() ?? 0);
                    slot.ApplyStain = obj[nameof(Slot.ApplyStain)]?.Value<bool>() ?? false;
                }
            }
        }

        var customize = obj[nameof(Customization)];
        if (customize == null)
        {
            design.CustomizeFlags = 0;
            design._customizeData = Customize.Default;
        }
        else
        {
            foreach (var choice in design.Customization)
            {
                var c = customize[choice.Index.ToDefaultName()];
                if (c == null)
                {
                    choice.Value = Customize.Default.Get(choice.Index);
                    choice.Apply = false;
                }
                else
                {
                    choice.Value = new CustomizeValue(c[nameof(Choice.Value)]?.Value<byte>() ?? Customize.Default.Get(choice.Index).Value);
                    choice.Apply = c[nameof(Choice.Apply)]?.Value<bool>() ?? false;
                }
            }
        }

        return true;
    }

    private static DateTimeOffset GetDateTime(long? value)
        => value == null ? DateTimeOffset.UtcNow : DateTimeOffset.FromUnixTimeSeconds(value.Value);
}
