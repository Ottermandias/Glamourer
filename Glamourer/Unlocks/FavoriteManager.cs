using Dalamud.Interface.Internal.Notifications;
using Glamourer.Services;
using Newtonsoft.Json;
using OtterGui.Classes;
using Penumbra.GameData.Structs;

namespace Glamourer.Unlocks;

public class FavoriteManager : ISavable
{
    private const    int              CurrentVersion = 1;
    private readonly SaveService      _saveService;
    private readonly HashSet<ItemId>  _favorites      = new();
    private readonly HashSet<StainId> _favoriteColors = new();

    public FavoriteManager(SaveService saveService)
    {
        _saveService = saveService;
        Load();
    }

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
                var load = JsonConvert.DeserializeObject<LoadStruct>(text);
                switch (load.Version)
                {
                    case 1:
                        _favorites.UnionWith(load.FavoriteItems.Select(i => (ItemId)i));
                        _favoriteColors.UnionWith(load.FavoriteColors.Select(i => (StainId)i));
                        break;
                    default: throw new Exception($"Unknown Version {load.Version}");
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
        var array = JsonConvert.DeserializeObject<uint[]>(text) ?? Array.Empty<uint>();
        _favorites.UnionWith(array.Select(i => (ItemId)i));
        Save();
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.FavoriteFile;

    private void Save()
        => _saveService.DelaySave(this);

    public void Save(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer)
        {
            Formatting = Formatting.Indented,
        };
        j.WriteStartObject();
        j.WritePropertyName(nameof(LoadStruct.Version));
        j.WriteValue(CurrentVersion);
        j.WritePropertyName(nameof(LoadStruct.FavoriteItems));
        j.WriteStartArray();
        foreach (var item in _favorites)
            j.WriteValue(item.Id);
        j.WriteEndArray();
        j.WritePropertyName(nameof(LoadStruct.FavoriteColors));
        j.WriteStartArray();
        foreach (var stain in _favoriteColors)
            j.WriteValue(stain.Id);
        j.WriteEndArray();
        j.WriteEndObject();
    }

    public bool TryAdd(EquipItem item)
        => TryAdd(item.ItemId);

    public bool TryAdd(ItemId item)
    {
        if (item.Id == 0 || !_favorites.Add(item))
            return false;

        Save();
        return true;
    }

    public bool TryAdd(Stain stain)
        => TryAdd(stain.RowIndex);

    public bool TryAdd(StainId stain)
    {
        if (stain.Id == 0 || !_favoriteColors.Add(stain))
            return false;

        Save();
        return true;
    }

    public bool Remove(EquipItem item)
        => Remove(item.ItemId);

    public bool Remove(ItemId item)
    {
        if (!_favorites.Remove(item))
            return false;

        Save();
        return true;
    }

    public bool Remove(Stain stain)
        => Remove(stain.RowIndex);

    public bool Remove(StainId stain)
    {
        if (!_favoriteColors.Remove(stain))
            return false;

        Save();
        return true;
    }

    public bool Contains(EquipItem item)
        => _favorites.Contains(item.ItemId);

    public bool Contains(Stain stain)
        => _favoriteColors.Contains(stain.RowIndex);

    public bool Contains(ItemId item)
        => _favorites.Contains(item);

    public bool Contains(StainId stain)
        => _favoriteColors.Contains(stain);

    private struct LoadStruct
    {
        public int    Version = CurrentVersion;
        public uint[] FavoriteItems  = Array.Empty<uint>();
        public byte[] FavoriteColors = Array.Empty<byte>();

        public LoadStruct()
        { }
    }
}
