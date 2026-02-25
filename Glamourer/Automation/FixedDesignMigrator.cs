using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using Glamourer.Interop;
using Luna;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Structs;
using Penumbra.String;

namespace Glamourer.Automation;

public sealed class FixedDesignMigrator(JobService jobs) : IRequiredService
{
    private List<(string Name, List<(string, JobGroup, bool)> Data)>? _migratedData;

    public void ConsumeMigratedData(ActorManager actors, DesignFileSystem designFileSystem, AutoDesignManager autoManager)
    {
        if (_migratedData == null)
            return;

        foreach (var (name, data) in _migratedData)
        {
            if (autoManager.Any(d => name == d.Name))
                continue;

            var id = ActorIdentifier.Invalid;
            if (ByteString.FromString(name, out var byteString))
            {
                id = actors.CreatePlayer(byteString, ushort.MaxValue);
                if (!id.IsValid)
                    id = actors.CreateRetainer(byteString, ActorIdentifier.RetainerType.Both);
            }

            if (!id.IsValid)
            {
                byteString = ByteString.FromSpanUnsafe("Mig Ration"u8, true, false, true);
                id         = actors.CreatePlayer(byteString, actors.Data.Worlds.First().Key);
                if (!id.IsValid)
                {
                    Glamourer.Messager.NotificationMessage($"Could not migrate fixed design {name}.", NotificationType.Error);
                    continue;
                }
            }

            autoManager.AddDesignSet(name, id);
            autoManager.SetState(autoManager.Count - 1, true);
            var set = autoManager[^1];
            foreach (var design in data.AsEnumerable().Reverse())
            {
                if (!designFileSystem.Find(design.Item1, out var child) || child is not IFileSystemData<Design> leaf)
                {
                    Glamourer.Messager.NotificationMessage($"Could not find design with path {design.Item1}, skipped fixed design.",
                        NotificationType.Warning);
                    continue;
                }

                autoManager.AddDesign(set, leaf.Value);
                autoManager.ChangeJobCondition(set, set.Designs.Count - 1, design.Item2);
                autoManager.ChangeApplicationType(set, set.Designs.Count - 1, design.Item3 ? ApplicationType.All : 0);
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
                Glamourer.Messager.NotificationMessage("Could not semi-migrate fixed design: No character name available.",
                    NotificationType.Warning);
                continue;
            }

            var path = obj["Path"]?.ToObject<string>() ?? string.Empty;
            if (path.Length == 0)
            {
                Glamourer.Messager.NotificationMessage("Could not semi-migrate fixed design: No design path available.",
                    NotificationType.Warning);
                continue;
            }

            var job = obj["JobGroups"]?.ToObject<int>() ?? -1;
            if (job < 0 || !jobs.JobGroups.TryGetValue((JobGroupId)job, out var group))
            {
                Glamourer.Messager.NotificationMessage("Could not semi-migrate fixed design: Invalid job group specified.",
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
