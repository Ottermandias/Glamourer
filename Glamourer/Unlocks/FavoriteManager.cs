using Dalamud.Interface.ImGuiNotification;
using Glamourer.Services;
using Newtonsoft.Json;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Unlocks;

public class FavoriteManager : ISavable
{
    private readonly record struct FavoriteHairStyle(Gender Gender, SubRace Race, CustomizeIndex Type, CustomizeValue Id)
    {
        public uint ToValue()
            => Id.Value | ((uint)Type << 8) | ((uint)Race << 16) | ((uint)Gender << 24);

        public FavoriteHairStyle(uint value)
            : this((Gender)((value >> 24) & 0xFF), (SubRace)((value >> 16) & 0xFF), (CustomizeIndex)((value >> 8) & 0xFF),
                (CustomizeValue)(value & 0xFF))
        { }
    }

    private const    int                        CurrentVersion = 1;
    private readonly SaveService                _saveService;
    private readonly HashSet<ItemId>            _favorites          = [];
    private readonly HashSet<StainId>           _favoriteColors     = [];
    private readonly HashSet<FavoriteHairStyle> _favoriteHairStyles = [];
    private readonly HashSet<BonusItemId>       _favoriteBonusItems = [];

    public FavoriteManager(SaveService saveService)
    {
        _saveService = saveService;
        Load();
    }

    public static bool TypeAllowed(CustomizeIndex type)
        => type switch
        {
            CustomizeIndex.Hairstyle => true,
            CustomizeIndex.FacePaint => true,
            _                        => false,
        };

    private void Load()
    {
        var file = _saveService.FileNames.FavoriteFile;
        if (!File.Exists(file))
            return;

        try
        {
            var text = File.ReadAllText(file);
            if (text.StartsWith('['))
            {
                LoadV0(text);
            }
            else
            {
                var load = JsonConvert.DeserializeObject<LoadIntermediary>(text);
                switch (load?.Version ?? 0)
                {
                    case 1:
                        _favorites.UnionWith(load!.FavoriteItems.Select(i => (ItemId)i));
                        _favoriteColors.UnionWith(load.FavoriteColors.Select(i => (StainId)i));
                        _favoriteHairStyles.UnionWith(load.FavoriteHairStyles.Select(t => new FavoriteHairStyle(t)));
                        _favoriteBonusItems.UnionWith(load.FavoriteBonusItems.Select(b => new BonusItemId(b)));
                        break;

                    default: throw new Exception($"Unknown Version {load?.Version ?? 0}");
                }
            }
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, "Could not read Favorite file.", NotificationType.Error);
        }
    }

    private void LoadV0(string text)
    {
        var array = JsonConvert.DeserializeObject<uint[]>(text) ?? [];
        _favorites.UnionWith(array.Select(i => (ItemId)i));
        Save();
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.FavoriteFile;

    private void Save()
        => _saveService.DelaySave(this);

    public void Save(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer);
        j.Formatting = Formatting.Indented;
        j.WriteStartObject();

        j.WritePropertyName(nameof(LoadIntermediary.Version));
        j.WriteValue(CurrentVersion);

        j.WritePropertyName(nameof(LoadIntermediary.FavoriteItems));
        j.WriteStartArray();
        foreach (var item in _favorites)
            j.WriteValue(item.Id);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.FavoriteColors));
        j.WriteStartArray();
        foreach (var stain in _favoriteColors)
            j.WriteValue(stain.Id);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.FavoriteHairStyles));
        j.WriteStartArray();
        foreach (var hairStyle in _favoriteHairStyles)
            j.WriteValue(hairStyle.ToValue());
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.FavoriteBonusItems));
        j.WriteStartArray();
        foreach (var item in _favoriteBonusItems)
            j.WriteValue(item.Id);
        j.WriteEndArray();

        j.WriteEndObject();
    }

    public bool TryAdd(EquipItem item)
    {
        if (item.Id.IsBonusItem)
            return TryAdd(item.Id.BonusItem);

        return TryAdd(item.ItemId);
    }

    public bool TryAdd(ItemId item)
    {
        if (item.Id == 0 || !_favorites.Add(item))
            return false;

        Save();
        return true;
    }

    public bool TryAdd(BonusItemId item)
    {
        if (item.Id == 0 || !_favoriteBonusItems.Add(item))
            return false;

        Save();
        return true;
    }

    public bool TryAdd(StainId stain)
    {
        if (stain.Id == 0 || !_favoriteColors.Add(stain))
            return false;

        Save();
        return true;
    }

    public bool TryAdd(Gender gender, SubRace race, CustomizeIndex type, CustomizeValue value)
    {
        if (!TypeAllowed(type) || !_favoriteHairStyles.Add(new FavoriteHairStyle(gender, race, type, value)))
            return false;

        Save();
        return true;
    }

    public bool Remove(EquipItem item)
    {
        if (item.Id.IsBonusItem)
            Remove(item.Id.BonusItem);
        return Remove(item.ItemId);
    }

    public bool Remove(ItemId item)
    {
        if (!_favorites.Remove(item))
            return false;

        Save();
        return true;
    }

    public bool Remove(BonusItemId item)
    {
        if (!_favoriteBonusItems.Remove(item))
            return false;

        Save();
        return true;
    }

    public bool Remove(StainId stain)
    {
        if (!_favoriteColors.Remove(stain))
            return false;

        Save();
        return true;
    }

    public bool Remove(Gender gender, SubRace race, CustomizeIndex type, CustomizeValue value)
    {
        if (!_favoriteHairStyles.Remove(new FavoriteHairStyle(gender, race, type, value)))
            return false;

        Save();
        return true;
    }

    public bool Contains(EquipItem item)
    {
        if (item.Id.IsBonusItem)
            return _favoriteBonusItems.Contains(item.Id.BonusItem);

        return _favorites.Contains(item.ItemId);
    }

    public bool Contains(StainId stain)
        => _favoriteColors.Contains(stain);

    public bool Contains(ItemId itemId)
        => _favorites.Contains(itemId);

    public bool Contains(BonusItemId bonusItemId)
        => _favoriteBonusItems.Contains(bonusItemId);

    public bool Contains(Gender gender, SubRace race, CustomizeIndex type, CustomizeValue value)
        => _favoriteHairStyles.Contains(new FavoriteHairStyle(gender, race, type, value));

    private class LoadIntermediary
    {
        public int      Version            = CurrentVersion;
        public uint[]   FavoriteItems      = [];
        public byte[]   FavoriteColors     = [];
        public uint[]   FavoriteHairStyles = [];
        public ushort[] FavoriteBonusItems = [];
    }
}
