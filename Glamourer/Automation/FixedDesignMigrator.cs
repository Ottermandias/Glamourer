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
    private readonly JobService                                            _jobs;
    private          Dictionary<string, (bool, List<(string, JobGroup)>)>? _migratedData;

    public FixedDesignMigrator(JobService jobs)
        => _jobs = jobs;

    public void ConsumeMigratedData(ActorService actors, DesignFileSystem designFileSystem, AutoDesignManager autoManager)
    {
        if (_migratedData == null)
            return;

        foreach (var data in _migratedData)
        {
            var enabled = data.Value.Item1;
            var name    = data.Key + (data.Value.Item1 ? " (Enabled)" : " (Disabled)");
            if (autoManager.Any(d => name == data.Key))
                continue;

            var id = ActorIdentifier.Invalid;
            if (ByteString.FromString(data.Key, out var byteString, false))
            {
                id = actors.AwaitedService.CreatePlayer(byteString, ushort.MaxValue);
                if (!id.IsValid)
                    id = actors.AwaitedService.CreateRetainer(byteString, ActorIdentifier.RetainerType.Both);
            }

            if (!id.IsValid)
            {
                byteString = ByteString.FromSpanUnsafe("Mig Ration"u8, true, false, true);
                id         = actors.AwaitedService.CreatePlayer(byteString, actors.AwaitedService.Data.Worlds.First().Key);
                enabled    = false;
                if (!id.IsValid)
                {
                    Glamourer.Chat.NotificationMessage($"Could not migrate fixed design {data.Key}.", "Error", NotificationType.Error);
                    continue;
                }
            }

            autoManager.AddDesignSet(name, id);
            autoManager.SetState(autoManager.Count - 1, enabled);
            var set = autoManager[^1];
            foreach (var design in data.Value.Item2)
            {
                if (!designFileSystem.Find(design.Item1, out var child) || child is not DesignFileSystem.Leaf leaf)
                {
                    Glamourer.Chat.NotificationMessage($"Could not find design with path {design.Item1}, skipped fixed design.", "Warning",
                        NotificationType.Warning);
                    continue;
                }

                autoManager.AddDesign(set, leaf.Value);
                autoManager.ChangeJobCondition(set, set.Designs.Count - 1, design.Item2);
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

        _migratedData = list.GroupBy(t => (t.Name, t.Enabled))
            .ToDictionary(kvp => kvp.Key.Name, kvp => (kvp.Key.Enabled, kvp.Select(k => (k.Path, k.Group)).ToList()));
    }
}
