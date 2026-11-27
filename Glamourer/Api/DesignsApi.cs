using System.Text.Json;
using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Glamourer.Designs;
using Glamourer.Designs.Links;
using Glamourer.GameData;
using Glamourer.Interop.Material;
using Glamourer.State;
using Luna;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;

namespace Glamourer.Api;

public class DesignsApi(
    ApiHelpers helpers,
    DesignManager designs,
    DesignLinkManager designLinks,
    StateManager stateManager,
    DesignColors color,
    DesignConverter converter)
    : IGlamourerApiDesigns, IApiService
{
    private const EquipFlag AllEquipItems  = (EquipFlag)0x00000FFF;
    private const EquipFlag AllEquipStains = (EquipFlag)0x00FFF000;

    public Dictionary<Guid, string> GetDesignList()
        => designs.Designs.ToDictionary(d => d.Identifier, d => d.Name);

    public Dictionary<Guid, (string DisplayName, string FullPath, uint DisplayColor, bool ShownInQdb)> GetDesignListExtended()
        => designs.Designs.ToDictionary(d => d.Identifier, d => (d.DisplayName, d.Path.CurrentPath, color.GetColor(d).Color, d.QuickDesign));

    public (string DisplayName, string FullPath, uint DisplayColor, bool ShowInQdb) GetExtendedDesignData(Guid designId)
        => designs.Designs.ByIdentifier(designId) is { } d
            ? (d.Name, d.Path.CurrentPath, color.GetColor(d).Color, d.QuickDesign)
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

        if (ParseDesign(designInput, nameof(AddDesign)) is not { } designBase)
            return (ApiHelpers.Return(GlamourerApiEc.CouldNotParse, args), Guid.Empty);

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

    public GlamourerApiEc UpdateDesign(Guid designId, string designInput, DesignUpdateFlag flags = DesignUpdateFlag.None)
    {
        var args = ApiHelpers.Args("DesignId", designId, "DesignData", designInput, "Flags", flags);
        if (designs.Designs.ByIdentifier(designId) is not { } design)
            return ApiHelpers.Return(GlamourerApiEc.DesignNotFound, args);

        if (design.WriteProtected())
            return ApiHelpers.Return(GlamourerApiEc.DesignWriteProtected, args);

        if (flags == DesignUpdateFlag.All)
            return ApiHelpers.Return(GlamourerApiEc.NothingDone, args);

        if (ParseDesign(designInput, nameof(UpdateDesign)) is not { } designBase)
            return ApiHelpers.Return(GlamourerApiEc.CouldNotParse, args);

        try
        {
            ApplyDesignUpdate(design, designBase, flags);
            return ApiHelpers.Return(GlamourerApiEc.Success, args);
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Unknown error updating design via IPC:\n{ex}");
            return ApiHelpers.Return(GlamourerApiEc.UnknownError, args);
        }
    }

    private void ApplyDesignUpdate(Design design, DesignBase source, DesignUpdateFlag flags)
    {
        // Handle materials via DesignEditor methods so SkipMaterials is respected and all material events fire.
        var sourceMaterials = source.Materials.ToArray();
        source.GetMaterialDataRef().Clear();

        var skipEquip   = flags.HasFlag(DesignUpdateFlag.SkipEquipment);
        var skipStains  = flags.HasFlag(DesignUpdateFlag.SkipStains);
        var skipCrests  = flags.HasFlag(DesignUpdateFlag.SkipCrests);
        var skipBonus   = flags.HasFlag(DesignUpdateFlag.SkipBonusItems);
        var skipMeta    = flags.HasFlag(DesignUpdateFlag.SkipMeta);
        var skipCust    = flags.HasFlag(DesignUpdateFlag.SkipCustomize);
        var skipParams  = flags.HasFlag(DesignUpdateFlag.SkipParameters);
        var syncAppRule = !flags.HasFlag(DesignUpdateFlag.SkipApplicationRules);

        var equipMask = (skipEquip ? (EquipFlag)0 : AllEquipItems)
                      | (skipStains ? (EquipFlag)0 : AllEquipStains);

        var restrictions = new ApplicationCollection(
            equipMask,
            skipBonus ? 0 : BonusExtensions.All,
            skipCust ? CustomizeFlag.BodyType : CustomizeFlagExtensions.AllRelevant,
            skipCrests ? 0 : CrestExtensions.AllRelevant,
            skipParams ? 0 : CustomizeParameterExtensions.All,
            skipMeta ? 0 : MetaExtensions.All);

        // ApplicationCollection always keeps BodyType enabled, so align it when SkipCustomize is set to avoid mutating BodyType.
        if (skipCust)
            source.GetDesignDataRef().Customize.Set(CustomizeIndex.BodyType, design.DesignData.Customize[CustomizeIndex.BodyType]);

        using (source.TemporarilyRestrictApplication(restrictions))
            designs.ApplyDesign(design, source);

        if (syncAppRule)
            SyncApplicationRules(design, source, skipEquip, skipStains, skipCrests, skipBonus, skipMeta, skipCust, skipParams);

        if (!flags.HasFlag(DesignUpdateFlag.SkipMaterials))
            SyncMaterials(design, sourceMaterials, syncAppRule);

        if (source is not Design sourceDesign)
            return;

        if (!flags.HasFlag(DesignUpdateFlag.SkipMods))
            SyncMods(design, sourceDesign);

        if (!flags.HasFlag(DesignUpdateFlag.SkipLinks))
            SyncLinks(design, sourceDesign);

        if (!flags.HasFlag(DesignUpdateFlag.SkipMetadata))
            SyncMetadata(design, sourceDesign);

        if (!flags.HasFlag(DesignUpdateFlag.SkipSettings))
            SyncSettings(design, sourceDesign);
    }

    private void SyncApplicationRules(Design design, DesignBase source, bool skipEquip, bool skipStains, bool skipCrests,
        bool skipBonus, bool skipMeta, bool skipCust, bool skipParams)
    {
        if (!skipEquip)
            foreach (var slot in EquipSlotExtensions.FullSlots)
                designs.ChangeApplyItem(design, slot, source.DoApplyEquip(slot));

        if (!skipStains)
            foreach (var slot in EquipSlotExtensions.FullSlots)
                designs.ChangeApplyStains(design, slot, source.DoApplyStain(slot));

        if (!skipBonus)
            foreach (var slot in BonusExtensions.AllFlags)
                designs.ChangeApplyBonusItem(design, slot, source.DoApplyBonusItem(slot));

        if (!skipCrests)
            foreach (var slot in CrestExtensions.AllRelevantSet)
                designs.ChangeApplyCrest(design, slot, source.DoApplyCrest(slot));

        if (!skipMeta)
            foreach (var meta in MetaExtensions.AllRelevant)
                designs.ChangeApplyMeta(design, meta, source.DoApplyMeta(meta));

        if (!skipCust)
            foreach (var idx in CustomizationExtensions.All)
                designs.ChangeApplyCustomize(design, idx, source.DoApplyCustomize(idx));

        if (!skipParams)
            foreach (var param in CustomizeParameterExtensions.AllFlags)
                designs.ChangeApplyParameter(design, param, source.DoApplyParameter(param));
    }

    private void SyncMaterials(Design design, (uint Key, MaterialValueDesign Value)[] sourceMaterials, bool syncApplyRules)
    {
        var sourceKeys = new HashSet<uint>(sourceMaterials.Length);
        foreach (var (key, value) in sourceMaterials)
        {
            if (syncApplyRules || value.Enabled)
                sourceKeys.Add(key);
        }

        foreach (var (key, _) in design.Materials.ToArray())
        {
            if (!sourceKeys.Contains(key))
                designs.ChangeMaterialValue(design, MaterialValueIndex.FromKey(key), null);
        }

        foreach (var (key, value) in sourceMaterials)
        {
            if (!syncApplyRules && !value.Enabled)
                continue;

            var idx = MaterialValueIndex.FromKey(key);
            designs.ChangeMaterialValue(design, idx, value.Value, value.Mode);
            if (design.GetMaterialDataRef().TryGetValue(idx, out var current))
            {
                if (current.Revert != value.Revert)
                    designs.ChangeMaterialRevert(design, idx, value.Revert);
                if (syncApplyRules && current.Enabled != value.Enabled)
                    designs.ChangeApplyMaterialValue(design, idx, value.Enabled);
            }
        }
    }

    private void SyncMods(Design design, Design sourceDesign)
    {
        foreach (var mod in design.AssociatedMods.Keys.ToArray())
        {
            if (!sourceDesign.AssociatedMods.ContainsKey(mod))
                designs.RemoveMod(design, mod);
        }

        foreach (var (mod, settings) in sourceDesign.AssociatedMods)
            designs.UpdateMod(design, mod, settings);
    }

    private void SyncLinks(Design design, Design sourceDesign)
    {
        for (var i = design.Links.Before.Count - 1; i >= 0; --i)
            designLinks.RemoveDesignLink(design, i, LinkOrder.Before);
        for (var i = design.Links.After.Count - 1; i >= 0; --i)
            designLinks.RemoveDesignLink(design, i, LinkOrder.After);

        AddLinks(design, sourceDesign.Links.Before, LinkOrder.Before);
        AddLinks(design, sourceDesign.Links.After,  LinkOrder.After);
    }

    private void AddLinks(Design design, IReadOnlyList<DesignLink> sourceLinks, LinkOrder order)
    {
        var targetList = order is LinkOrder.Before ? design.Links.Before : design.Links.After;
        foreach (var link in sourceLinks)
        {
            var countBefore = targetList.Count;
            designLinks.AddDesignLink(design, link.Link, order);
            if (targetList.Count > countBefore)
            {
                var idx = targetList.Count - 1;
                designLinks.ChangeApplicationType(design, idx, order, link.Type);
                designLinks.ChangeConditions(design, idx, order, link.Conditions);
            }
        }
    }

    private void SyncMetadata(Design design, Design sourceDesign)
    {
        if (!string.IsNullOrEmpty(sourceDesign.Name))
            designs.Rename(design, sourceDesign.Name);

        designs.ChangeDescription(design, sourceDesign.Description);
        designs.ChangeColor(design, sourceDesign.Color);
        designs.SetQuickDesign(design, sourceDesign.QuickDesign);

        var sourceTags = new HashSet<string>(sourceDesign.Tags, StringComparer.Ordinal);
        for (var i = design.Tags.Length - 1; i >= 0; --i)
        {
            if (!sourceTags.Contains(design.Tags[i]))
                designs.RemoveTag(design, i);
        }

        foreach (var tag in sourceDesign.Tags)
            designs.AddTag(design, tag);
    }

    private void SyncSettings(Design design, Design sourceDesign)
    {
        designs.ChangeForcedRedraw(design, sourceDesign.ForcedRedraw);
        designs.ChangeResetAdvancedDyes(design, sourceDesign.ResetAdvancedDyes);
        designs.ChangeRevertAdvancedDyes(design, sourceDesign.RevertAdvancedDyes);
        designs.ChangeResetTemporarySettings(design, sourceDesign.ResetTemporarySettings);
    }

    private DesignBase? ParseDesign(string designInput, string caller)
    {
        if (string.IsNullOrWhiteSpace(designInput))
            return null;

        if (designInput.AsSpan().TrimStart().StartsWith('{'))
            try
            {
                var jObj = JsonElement.Parse(designInput);
                return converter.FromJsonElement(jObj, true, true);
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Failure parsing data for {caller} due to\n{ex}");
                return null;
            }

        return converter.FromBase64(designInput, true, true, out _);
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
            ? Encoding.UTF8.GetString(converter.ShareBase64(design))
            : null;

    public JObject? GetDesignJObject(Guid designId)
        => designs.Designs.ByIdentifier(designId) is { } design
            ? converter.ShareJObject(design).ToToken() as JObject
            : null;
}
