using Glamourer.Designs.Links;
using Glamourer.Services;
using Glamourer.State;
using OtterGui.Services;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop.Penumbra;

public class ModSettingApplier(PenumbraService penumbra, Configuration config, ObjectManager objects, CollectionOverrideService overrides)
    : IService
{
    public void HandleStateApplication(ActorState state, MergedDesign design)
    {
        if (!config.AlwaysApplyAssociatedMods || design.AssociatedMods.Count == 0)
            return;

        objects.Update();
        if (!objects.TryGetValue(state.Identifier, out var data))
        {
            Glamourer.Log.Verbose(
                $"[Mod Applier] No mod settings applied because no actor for {state.Identifier} could be found to associate collection.");
            return;
        }

        var collections = new HashSet<Guid>();

        foreach (var actor in data.Objects)
        {
            var (collection, _, overridden) = overrides.GetCollection(actor, state.Identifier);
            if (collection == Guid.Empty)
                continue;

            if (!collections.Add(collection))
                continue;

            foreach (var (mod, setting) in design.AssociatedMods)
            {
                var message = penumbra.SetMod(mod, setting, collection);
                if (message.Length > 0)
                    Glamourer.Log.Verbose($"[Mod Applier] Error applying mod settings: {message}");
                else
                    Glamourer.Log.Verbose(
                        $"[Mod Applier] Set mod settings for {mod.DirectoryName} in {collection}{(overridden ? " (overridden by settings)" : string.Empty)}.");
            }
        }
    }

    public (List<string> Messages, int Applied, Guid Collection, string Name, bool Overridden) ApplyModSettings(IReadOnlyDictionary<Mod, ModSettings> settings, Actor actor)
    {
        var (collection, name, overridden) = overrides.GetCollection(actor);
        if (collection == Guid.Empty)
            return ([$"{actor.Utf8Name} uses no mods."], 0, Guid.Empty, string.Empty, false);

        var messages    = new List<string>();
        var appliedMods = 0;
        foreach (var (mod, setting) in settings)
        {
            var message = penumbra.SetMod(mod, setting, collection);
            if (message.Length > 0)
                messages.Add($"Error applying mod settings: {message}");
            else
                ++appliedMods;
        }

        return (messages, appliedMods, collection, name, overridden);
    }
}
