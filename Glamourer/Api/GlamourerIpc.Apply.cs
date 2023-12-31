using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop.Structs;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelApplyAll                          = "Glamourer.ApplyAll";
    public const string LabelApplyAllToCharacter               = "Glamourer.ApplyAllToCharacter";
    public const string LabelApplyOnlyEquipment                = "Glamourer.ApplyOnlyEquipment";
    public const string LabelApplyOnlyEquipmentToCharacter     = "Glamourer.ApplyOnlyEquipmentToCharacter";
    public const string LabelApplyOnlyCustomization            = "Glamourer.ApplyOnlyCustomization";
    public const string LabelApplyOnlyCustomizationToCharacter = "Glamourer.ApplyOnlyCustomizationToCharacter";

    public const string LabelApplyAllLock                          = "Glamourer.ApplyAllLock";
    public const string LabelApplyAllToCharacterLock               = "Glamourer.ApplyAllToCharacterLock";
    public const string LabelApplyOnlyEquipmentLock                = "Glamourer.ApplyOnlyEquipmentLock";
    public const string LabelApplyOnlyEquipmentToCharacterLock     = "Glamourer.ApplyOnlyEquipmentToCharacterLock";
    public const string LabelApplyOnlyCustomizationLock            = "Glamourer.ApplyOnlyCustomizationLock";
    public const string LabelApplyOnlyCustomizationToCharacterLock = "Glamourer.ApplyOnlyCustomizationToCharacterLock";

    public const string LabelApplyByGuid            = "Glamourer.ApplyByGuid";
    public const string LabelApplyByGuidToCharacter = "Glamourer.ApplyByGuidToCharacter";

    private readonly ActionProvider<string, string>     _applyAllProvider;
    private readonly ActionProvider<string, Character?> _applyAllToCharacterProvider;
    private readonly ActionProvider<string, string>     _applyOnlyEquipmentProvider;
    private readonly ActionProvider<string, Character?> _applyOnlyEquipmentToCharacterProvider;
    private readonly ActionProvider<string, string>     _applyOnlyCustomizationProvider;
    private readonly ActionProvider<string, Character?> _applyOnlyCustomizationToCharacterProvider;

    private readonly ActionProvider<string, string, uint>     _applyAllProviderLock;
    private readonly ActionProvider<string, Character?, uint> _applyAllToCharacterProviderLock;
    private readonly ActionProvider<string, string, uint>     _applyOnlyEquipmentProviderLock;
    private readonly ActionProvider<string, Character?, uint> _applyOnlyEquipmentToCharacterProviderLock;
    private readonly ActionProvider<string, string, uint>     _applyOnlyCustomizationProviderLock;
    private readonly ActionProvider<string, Character?, uint> _applyOnlyCustomizationToCharacterProviderLock;

    private readonly ActionProvider<Guid, string>     _applyByGuidProvider;
    private readonly ActionProvider<Guid, Character?> _applyByGuidToCharacterProvider;

    public static ActionSubscriber<string, string> ApplyAllSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyAll);

    public static ActionSubscriber<string, Character?> ApplyAllToCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyAllToCharacter);

    public static ActionSubscriber<string, string> ApplyOnlyEquipmentSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyOnlyEquipment);

    public static ActionSubscriber<string, Character?> ApplyOnlyEquipmentToCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyOnlyEquipmentToCharacter);

    public static ActionSubscriber<string, string> ApplyOnlyCustomizationSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyOnlyCustomization);

    public static ActionSubscriber<string, Character?> ApplyOnlyCustomizationToCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyOnlyCustomizationToCharacter);

    public static ActionSubscriber<Guid, string> ApplyByGuidSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyByGuid);

    public static ActionSubscriber<Guid, Character?> ApplyByGuidToCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyByGuidToCharacter);

    public void ApplyAll(string base64, string characterName)
        => ApplyDesign(_designConverter.FromBase64(base64, true, true, out var version), FindActors(characterName), version, 0);

    public void ApplyAllToCharacter(string base64, Character? character)
        => ApplyDesign(_designConverter.FromBase64(base64, true, true, out var version), FindActors(character), version, 0);

    public void ApplyOnlyEquipment(string base64, string characterName)
        => ApplyDesign(_designConverter.FromBase64(base64, false, true, out var version), FindActors(characterName), version, 0);

    public void ApplyOnlyEquipmentToCharacter(string base64, Character? character)
        => ApplyDesign(_designConverter.FromBase64(base64, false, true, out var version), FindActors(character), version, 0);

    public void ApplyOnlyCustomization(string base64, string characterName)
        => ApplyDesign(_designConverter.FromBase64(base64, true, false, out var version), FindActors(characterName), version, 0);

    public void ApplyOnlyCustomizationToCharacter(string base64, Character? character)
        => ApplyDesign(_designConverter.FromBase64(base64, true, false, out var version), FindActors(character), version, 0);


    public void ApplyAllLock(string base64, string characterName, uint lockCode)
        => ApplyDesign(_designConverter.FromBase64(base64, true, true, out var version), FindActors(characterName), version, lockCode);

    public void ApplyAllToCharacterLock(string base64, Character? character, uint lockCode)
        => ApplyDesign(_designConverter.FromBase64(base64, true, true, out var version), FindActors(character), version, lockCode);

    public void ApplyOnlyEquipmentLock(string base64, string characterName, uint lockCode)
        => ApplyDesign(_designConverter.FromBase64(base64, false, true, out var version), FindActors(characterName), version, lockCode);

    public void ApplyOnlyEquipmentToCharacterLock(string base64, Character? character, uint lockCode)
        => ApplyDesign(_designConverter.FromBase64(base64, false, true, out var version), FindActors(character), version, lockCode);

    public void ApplyOnlyCustomizationLock(string base64, string characterName, uint lockCode)
        => ApplyDesign(_designConverter.FromBase64(base64, true, false, out var version), FindActors(characterName), version, lockCode);

    public void ApplyOnlyCustomizationToCharacterLock(string base64, Character? character, uint lockCode)
        => ApplyDesign(_designConverter.FromBase64(base64, true, false, out var version), FindActors(character), version, lockCode);


    public void ApplyByGuid(Guid identifier, string characterName)
        => ApplyDesignByGuid(identifier, FindActors(characterName), 0);

    public void ApplyByGuidToCharacter(Guid identifier, Character? character)
        => ApplyDesignByGuid(identifier, FindActors(character), 0);

    private void ApplyDesign(DesignBase? design, IEnumerable<ActorIdentifier> actors, byte version, uint lockCode)
    {
        if (design == null)
            return;

        var hasModelId = version >= 3;
        _objects.Update();
        foreach (var id in actors)
        {
            if (!_stateManager.TryGetValue(id, out var state))
            {
                var data = _objects.TryGetValue(id, out var d) ? d : ActorData.Invalid;
                if (!data.Valid || !_stateManager.GetOrCreate(id, data.Objects[0], out state))
                    continue;
            }

            if ((hasModelId || state.ModelData.ModelId == 0) && state.CanUnlock(lockCode))
            {
                _stateManager.ApplyDesign(design, state, StateChanged.Source.Ipc, lockCode);
                state.Lock(lockCode);
            }
        }
    }

    private void ApplyDesignByGuid(Guid identifier, IEnumerable<ActorIdentifier> actors, uint lockCode)
        => ApplyDesign(_designManager.Designs.FirstOrDefault(x => x.Identifier == identifier), actors, DesignConverter.Version, lockCode);
}
