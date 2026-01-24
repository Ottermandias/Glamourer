using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Glamourer.Designs;
using Glamourer.State;
using Luna;
using Newtonsoft.Json.Linq;

namespace Glamourer.Api;

public class DesignsApi(
    ApiHelpers helpers,
    DesignManager designs,
    StateManager stateManager,
    DesignFileSystem fileSystem,
    DesignColors color,
    DesignConverter converter)
    : IGlamourerApiDesigns, IApiService
{
    public Dictionary<Guid, string> GetDesignList()
        => designs.Designs.ToDictionary(d => d.Identifier, d => d.Name.Text);

    public Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> GetDesignListExtended()
        => fileSystem.ToDictionary(kvp => kvp.Key.Identifier,
            kvp => (kvp.Key.Name.Text, kvp.Value.FullName(), color.GetColor(kvp.Key), kvp.Key.QuickDesign));

    public (string DisplayName, string FullPath, uint DisplayColor, bool ShowInQdb) GetExtendedDesignData(Guid designId)
        => designs.Designs.ByIdentifier(designId) is { } d
            ? (d.Name.Text, fileSystem.TryGetValue(d, out var leaf) ? leaf.FullName() : d.Name.Text, color.GetColor(d), d.QuickDesign)
            : (string.Empty, string.Empty, 0, false);

    public GlamourerApiEc ApplyDesign(Guid designId, int objectIndex, uint key, ApplyFlag flags)
    {
        var args   = ApiHelpers.Args("Design", designId, "Index", objectIndex, "Key", key, "Flags", flags);
        var design = designs.Designs.ByIdentifier(designId);
        if (design == null)
            return ApiHelpers.Return(GlamourerApiEc.DesignNotFound, args);

        if (helpers.FindState(objectIndex) is not { } state)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (!state.CanUnlock(key))
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        ApplyDesign(state, design, key, flags);
        ApiHelpers.Lock(state, key, flags);
        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    private void ApplyDesign(ActorState state, Design design, uint key, ApplyFlag flags)
    {
        var once = (flags & ApplyFlag.Once) != 0;
        var settings = new ApplySettings(Source: once ? StateSource.IpcManual : StateSource.IpcFixed, Key: key, MergeLinks: true,
            ResetMaterials: !once && key != 0, IsFinal: true);

        using var restrict = ApiHelpers.Restrict(design, flags);
        stateManager.ApplyDesign(state, design, settings);
    }

    public GlamourerApiEc ApplyDesignName(Guid designId, string playerName, uint key, ApplyFlag flags)
    {
        var args   = ApiHelpers.Args("Design", designId, "Name", playerName, "Key", key, "Flags", flags);
        var design = designs.Designs.ByIdentifier(designId);
        if (design == null)
            return ApiHelpers.Return(GlamourerApiEc.DesignNotFound, args);

        var any         = false;
        var anyUnlocked = false;
        foreach (var state in helpers.FindStates(playerName))
        {
            any = true;
            if (!state.CanUnlock(key))
                continue;

            anyUnlocked = true;
            ApplyDesign(state, design, key, flags);
            ApiHelpers.Lock(state, key, flags);
        }

        if (!any)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);
        if (!anyUnlocked)
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public (GlamourerApiEc, Guid) AddDesign(string designInput, string name)
    {
        var args = ApiHelpers.Args("DesignData", designInput, "Name", name);

        if (converter.FromBase64(designInput, true, true, out _) is not { } designBase)
            try
            {
                var jObj = JObject.Parse(designInput);
                designBase = converter.FromJObject(jObj, true, true);
                if (designBase is null)
                    return (ApiHelpers.Return(GlamourerApiEc.CouldNotParse, args), Guid.Empty);
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Failure parsing data for AddDesign due to\n{ex}");
                return (ApiHelpers.Return(GlamourerApiEc.CouldNotParse, args), Guid.Empty);
            }

        try
        {
            var design = designBase is Design d
                ? designs.CreateClone(d,          name, true)
                : designs.CreateClone(designBase, name, true);
            return (ApiHelpers.Return(GlamourerApiEc.Success, args), design.Identifier);
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Unknown error creating design via IPC:\n{ex}");
            return (ApiHelpers.Return(GlamourerApiEc.UnknownError, args), Guid.Empty);
        }
    }

    public GlamourerApiEc DeleteDesign(Guid designId)
    {
        var args = ApiHelpers.Args("DesignId", designId);
        if (designs.Designs.ByIdentifier(designId) is not { } design)
            return ApiHelpers.Return(GlamourerApiEc.NothingDone, args);

        designs.Delete(design);
        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public string? GetDesignBase64(Guid designId)
        => designs.Designs.ByIdentifier(designId) is { } design
            ? converter.ShareBase64(design)
            : null;

    public JObject? GetDesignJObject(Guid designId)
        => designs.Designs.ByIdentifier(designId) is { } design
            ? converter.ShareJObject(design)
            : null;
}
