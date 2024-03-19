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

        var collections = new HashSet<string>();

        foreach (var actor in data.Objects)
        {
            var (collection, overridden) = overrides.GetCollection(actor, state.Identifier);
            if (collection.Length == 0)
            {
                Glamourer.Log.Verbose($"[Mod Applier] Could not obtain associated collection for {actor.Utf8Name}.");
                continue;
            }

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

    public (List<string> Messages, int Applied, string Collection, bool Overridden) ApplyModSettings(IReadOnlyDictionary<Mod, ModSettings> settings, Actor actor)
    {
        var (collection, overridden) = overrides.GetCollection(actor);
        if (collection.Length <= 0)
            return ([$"Could not obtain associated collection for {actor.Utf8Name}."], 0, string.Empty, false);

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

        return (messages, appliedMods, collection, overridden);
    }
}
