using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Glamourer.Designs;
using Glamourer.State;
using Newtonsoft.Json.Linq;
using OtterGui.Services;

namespace Glamourer.Api;

public class DesignsApi(ApiHelpers helpers, DesignManager designs, StateManager stateManager, DesignFileSystem fileSystem, DesignColors color, DesignConverter converter)
    : IGlamourerApiDesigns, IApiService
{
    public Dictionary<Guid, string> GetDesignList()
        => designs.Designs.ToDictionary(d => d.Identifier, d => d.Name.Text);

    public Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> GetDesignListExtended()
        => designs.Designs.ToDictionary(d => d.Identifier,
            d => (d.Name.Text, fileSystem.FindLeaf(d, out var leaf) ? leaf.FullName() : d.Name.Text, color.GetColor(d), d.QuickDesign));

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

    public (GlamourerApiEc, Guid?) AddDesign(string designInput, string name)
    {
        var args = ApiHelpers.Args("Design Input", designInput);

        DesignBase? designBase = converter.FromBase64(designInput, true, true, out _);
        if (designBase == null)
        {
            try
            {
                var jObj = JObject.Parse(designInput);
                designBase = converter.FromJObject(jObj, true, true);
            }
            catch
            {
                return (ApiHelpers.Return(GlamourerApiEc.CouldNotParse, args), null);
            }
        }

        if (designBase == null)
            return (ApiHelpers.Return(GlamourerApiEc.CouldNotParse, args), null);

        Design design = designs.CreateClone(designBase, name, true);

        return (ApiHelpers.Return(GlamourerApiEc.Success, args), design.Identifier);
    }
}
