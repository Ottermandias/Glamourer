using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Designs;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.Structs;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;
using Penumbra.String;

namespace Glamourer.Automation;

public class FixedDesignMigrator
{
    private readonly JobService                                                _jobs;
    private          List<(string Name, List<(string, JobGroup, bool)> Data)>? _migratedData;

    public FixedDesignMigrator(JobService jobs)
        => _jobs = jobs;

    public void ConsumeMigratedData(ActorService actors, DesignFileSystem designFileSystem, AutoDesignManager autoManager)
    {
        if (_migratedData == null)
            return;

        foreach (var data in _migratedData)
        {
            var allEnabled = true;
            var name       = data.Name;
            if (autoManager.Any(d => name == d.Name))
                continue;

            var id = ActorIdentifier.Invalid;
            if (ByteString.FromString(data.Name, out var byteString, false))
            {
                id = actors.AwaitedService.CreatePlayer(byteString, ushort.MaxValue);
                if (!id.IsValid)
                    id = actors.AwaitedService.CreateRetainer(byteString, ActorIdentifier.RetainerType.Both);
            }

            if (!id.IsValid)
            {
                byteString = ByteString.FromSpanUnsafe("Mig Ration"u8, true, false, true);
                id         = actors.AwaitedService.CreatePlayer(byteString, actors.AwaitedService.Data.Worlds.First().Key);
                if (!id.IsValid)
                {
                    Glamourer.Chat.NotificationMessage($"Could not migrate fixed design {data.Name}.", "Error", NotificationType.Error);
                    allEnabled = false;
                    continue;
                }
            }

            autoManager.AddDesignSet(name, id);
            autoManager.SetState(autoManager.Count - 1, allEnabled);
            var set = autoManager[^1];
            foreach (var design in data.Data.AsEnumerable().Reverse())
            {
                if (!designFileSystem.Find(design.Item1, out var child) || child is not DesignFileSystem.Leaf leaf)
                {
                    Glamourer.Chat.NotificationMessage($"Could not find design with path {design.Item1}, skipped fixed design.", "Warning",
                        NotificationType.Warning);
                    continue;
                }

                autoManager.AddDesign(set, leaf.Value);
                autoManager.ChangeJobCondition(set, set.Designs.Count - 1, design.Item2);
                autoManager.ChangeApplicationType(set, set.Designs.Count - 1, design.Item3 ? AutoDesign.Type.All : 0);
            }
        }
    }

    public void Migrate(JToken? data)
    {
        if (data is not JArray array)
            return;

        var list = new List<(string Name, string Path, JobGroup Group, bool Enabled)>();
        foreach (var obj in array)
        {
            var name = obj["Name"]?.ToObject<string>() ?? string.Empty;
            if (name.Length == 0)
            {
                Glamourer.Chat.NotificationMessage("Could not semi-migrate fixed design: No character name available.", "Warning",
                    NotificationType.Warning);
                continue;
            }

            var path = obj["Path"]?.ToObject<string>() ?? string.Empty;
            if (path.Length == 0)
            {
                Glamourer.Chat.NotificationMessage("Could not semi-migrate fixed design: No design path available.", "Warning",
                    NotificationType.Warning);
                continue;
            }

            var job = obj["JobGroups"]?.ToObject<int>() ?? -1;
            if (job < 0 || !_jobs.JobGroups.TryGetValue((ushort)job, out var group))
            {
                Glamourer.Chat.NotificationMessage("Could not semi-migrate fixed design: Invalid job group specified.", "Warning",
                    NotificationType.Warning);
                continue;
            }

            var enabled = obj["Enabled"]?.ToObject<bool>() ?? false;
            list.Add((name, path, group, enabled));
        }

        _migratedData = list.GroupBy(t => t.Name)
            .Select(kvp => (kvp.Key, kvp.Select(k => (k.Path, k.Group, k.Enabled)).ToList()))
            .ToList();
    }
}
