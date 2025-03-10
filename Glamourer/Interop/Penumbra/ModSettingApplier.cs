using Glamourer.Designs.Links;
using Glamourer.Services;
using Glamourer.State;
using OtterGui.Services;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop.Penumbra;

public class ModSettingApplier(PenumbraService penumbra, PenumbraAutoRedrawSkip autoRedrawSkip, Configuration config, ObjectManager objects, CollectionOverrideService overrides)
    : IService
{
    private readonly HashSet<Guid> _collectionTracker = [];

    public void HandleStateApplication(ActorState state, MergedDesign design, StateSource source, bool skipAutoRedraw, bool respectManual)
    {
        if (!config.AlwaysApplyAssociatedMods || (design.AssociatedMods.Count == 0 && !design.ResetTemporarySettings))
            return;

        objects.Update();
        if (!objects.TryGetValue(state.Identifier, out var data))
        {
            Glamourer.Log.Verbose(
                $"[Mod Applier] No mod settings applied because no actor for {state.Identifier.Incognito(null)} could be found to associate collection.");
            return;
        }

        _collectionTracker.Clear();
        using var skip = autoRedrawSkip.SkipAutoUpdates(skipAutoRedraw);
        foreach (var actor in data.Objects)
        {
            var (collection, _, overridden) = overrides.GetCollection(actor, state.Identifier);
            if (collection == Guid.Empty)
                continue;

            if (!_collectionTracker.Add(collection))
                continue;

            var index = ResetOldSettings(collection, actor, source, design.ResetTemporarySettings, respectManual);
            foreach (var (mod, setting) in design.AssociatedMods)
            {
                var message = penumbra.SetMod(mod, setting, source, respectManual, collection, index);
                if (message.Length > 0)
                    Glamourer.Log.Verbose($"[Mod Applier] Error applying mod settings: {message}");
                else
                    Glamourer.Log.Verbose(
                        $"[Mod Applier] Set mod settings for {mod.DirectoryName} in {collection}{(overridden ? " (overridden by settings)" : string.Empty)}.");
            }
        }
    }

    public (List<string> Messages, int Applied, Guid Collection, string Name, bool Overridden) ApplyModSettings(
        IReadOnlyDictionary<Mod, ModSettings> settings, Actor actor, StateSource source, bool resetOther)
    {
        var (collection, name, overridden) = overrides.GetCollection(actor);
        if (collection == Guid.Empty)
            return ([$"{actor.Utf8Name} uses no mods."], 0, Guid.Empty, string.Empty, false);

        var messages    = new List<string>();
        var appliedMods = 0;

        var index = ResetOldSettings(collection, actor, source, resetOther, true);
        foreach (var (mod, setting) in settings)
        {
            var message = penumbra.SetMod(mod, setting, source, false, collection, index);
            if (message.Length > 0)
                messages.Add($"Error applying mod settings: {message}");
            else
                ++appliedMods;
        }

        return (messages, appliedMods, collection, name, overridden);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ObjectIndex? ResetOldSettings(Guid collection, Actor actor, StateSource source, bool resetOther, bool respectManual)
    {
        ObjectIndex? index = actor.Valid ? actor.Index : null;
        if (!resetOther)
            return index;

        if (index == null)
        {
            penumbra.RemoveAllTemporarySettings(collection, source);
            if (!respectManual && source.IsFixed())
                penumbra.RemoveAllTemporarySettings(collection, StateSource.Manual);
        }
        else
        {
            penumbra.RemoveAllTemporarySettings(index.Value, source);
            if (!respectManual && source.IsFixed())
                penumbra.RemoveAllTemporarySettings(index.Value, StateSource.Manual);
        }
        return index;
    }
}
