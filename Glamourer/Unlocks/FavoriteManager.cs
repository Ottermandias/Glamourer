using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using Penumbra.GameData.Structs;

namespace Glamourer.Unlocks;

public class FavoriteManager : ISavable
{
    private readonly SaveService     _saveService;
    private readonly HashSet<ItemId> _favorites = new();

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
            var text  = File.ReadAllText(file);
            var array = JsonConvert.DeserializeObject<uint[]>(text) ?? Array.Empty<uint>();
            _favorites.UnionWith(array.Select(i => (ItemId)i));
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, "Could not read Favorite file.", NotificationType.Error);
        }
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
        j.WriteStartArray();
        foreach (var item in _favorites)
            j.WriteValue(item.Id);
        j.WriteEndArray();
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

    public bool Remove(EquipItem item)
        => Remove(item.ItemId);

    public bool Remove(ItemId item)
    {
        if (!_favorites.Remove(item))
            return false;

        Save();
        return true;
    }

    public IEnumerator<ItemId> GetEnumerator()
        => _favorites.GetEnumerator();

    public int Count
        => _favorites.Count;

    public bool Contains(EquipItem item)
        => _favorites.Contains(item.ItemId);

    public bool Contains(ItemId item)
        => _favorites.Contains(item);
}
