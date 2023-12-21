using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.State;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.String;

namespace Glamourer.Api;

public partial class GlamourerIpc : IDisposable
{
    public const int CurrentApiVersionMajor = 0;
    public const int CurrentApiVersionMinor = 4;

    private readonly StateManager      _stateManager;
    private readonly ObjectManager     _objects;
    private readonly ActorManager      _actors;
    private readonly DesignConverter   _designConverter;
    private readonly AutoDesignApplier _autoDesignApplier;

    public GlamourerIpc(DalamudPluginInterface pi, StateManager stateManager, ObjectManager objects, ActorManager actors,
        DesignConverter designConverter, StateChanged stateChangedEvent, GPoseService gPose, AutoDesignApplier autoDesignApplier)
    {
        _stateManager        = stateManager;
        _objects             = objects;
        _actors              = actors;
        _designConverter     = designConverter;
        _autoDesignApplier   = autoDesignApplier;
        _gPose               = gPose;
        _stateChangedEvent   = stateChangedEvent;
        _apiVersionProvider  = new FuncProvider<int>(pi, LabelApiVersion, ApiVersion);
        _apiVersionsProvider = new FuncProvider<(int Major, int Minor)>(pi, LabelApiVersions, ApiVersions);

        _getAllCustomizationProvider = new FuncProvider<string, string?>(pi, LabelGetAllCustomization, GetAllCustomization);
        _getAllCustomizationFromCharacterProvider =
            new FuncProvider<Character?, string?>(pi, LabelGetAllCustomizationFromCharacter, GetAllCustomizationFromCharacter);

        _applyAllProvider            = new ActionProvider<string, string>(pi, LabelApplyAll, ApplyAll);
        _applyAllToCharacterProvider = new ActionProvider<string, Character?>(pi, LabelApplyAllToCharacter, ApplyAllToCharacter);
        _applyOnlyEquipmentProvider  = new ActionProvider<string, string>(pi, LabelApplyOnlyEquipment, ApplyOnlyEquipment);
        _applyOnlyEquipmentToCharacterProvider =
            new ActionProvider<string, Character?>(pi, LabelApplyOnlyEquipmentToCharacter, ApplyOnlyEquipmentToCharacter);
        _applyOnlyCustomizationProvider = new ActionProvider<string, string>(pi, LabelApplyOnlyCustomization, ApplyOnlyCustomization);
        _applyOnlyCustomizationToCharacterProvider =
            new ActionProvider<string, Character?>(pi, LabelApplyOnlyCustomizationToCharacter, ApplyOnlyCustomizationToCharacter);

        _applyAllProviderLock = new ActionProvider<string, string, uint>(pi, LabelApplyAllLock, ApplyAllLock);
        _applyAllToCharacterProviderLock =
            new ActionProvider<string, Character?, uint>(pi, LabelApplyAllToCharacterLock, ApplyAllToCharacterLock);
        _applyOnlyEquipmentProviderLock = new ActionProvider<string, string, uint>(pi, LabelApplyOnlyEquipmentLock, ApplyOnlyEquipmentLock);
        _applyOnlyEquipmentToCharacterProviderLock =
            new ActionProvider<string, Character?, uint>(pi, LabelApplyOnlyEquipmentToCharacterLock, ApplyOnlyEquipmentToCharacterLock);
        _applyOnlyCustomizationProviderLock =
            new ActionProvider<string, string, uint>(pi, LabelApplyOnlyCustomizationLock, ApplyOnlyCustomizationLock);
        _applyOnlyCustomizationToCharacterProviderLock =
            new ActionProvider<string, Character?, uint>(pi, LabelApplyOnlyCustomizationToCharacterLock, ApplyOnlyCustomizationToCharacterLock);

        _revertProvider              = new ActionProvider<string>(pi, LabelRevert, Revert);
        _revertCharacterProvider     = new ActionProvider<Character?>(pi, LabelRevertCharacter, RevertCharacter);
        _revertProviderLock          = new ActionProvider<string, uint>(pi, LabelRevertLock, RevertLock);
        _revertCharacterProviderLock = new ActionProvider<Character?, uint>(pi, LabelRevertCharacterLock, RevertCharacterLock);
        _unlockNameProvider          = new FuncProvider<string, uint, bool>(pi, LabelUnlockName, Unlock);
        _unlockProvider              = new FuncProvider<Character?, uint, bool>(pi, LabelUnlock, Unlock);
        _revertToAutomationProvider  = new FuncProvider<string, uint, bool>(pi, LabelRevertToAutomation, RevertToAutomation);
        _revertToAutomationCharacterProvider =
            new FuncProvider<Character?, uint, bool>(pi, LabelRevertToAutomationCharacter, RevertToAutomation);

        _stateChangedProvider = new EventProvider<StateChanged.Type, nint, Lazy<string>>(pi, LabelStateChanged);
        _gPoseChangedProvider = new EventProvider<bool>(pi, LabelGPoseChanged);

        _stateChangedEvent.Subscribe(OnStateChanged, StateChanged.Priority.GlamourerIpc);
        _gPose.Subscribe(OnGPoseChanged, GPoseService.Priority.GlamourerIpc);
    }

    public void Dispose()
    {
        _apiVersionProvider.Dispose();
        _apiVersionsProvider.Dispose();

        _getAllCustomizationProvider.Dispose();
        _getAllCustomizationFromCharacterProvider.Dispose();

        _applyAllProvider.Dispose();
        _applyAllToCharacterProvider.Dispose();
        _applyOnlyEquipmentProvider.Dispose();
        _applyOnlyEquipmentToCharacterProvider.Dispose();
        _applyOnlyCustomizationProvider.Dispose();
        _applyOnlyCustomizationToCharacterProvider.Dispose();
        _applyAllProviderLock.Dispose();
        _applyAllToCharacterProviderLock.Dispose();
        _applyOnlyEquipmentProviderLock.Dispose();
        _applyOnlyEquipmentToCharacterProviderLock.Dispose();
        _applyOnlyCustomizationProviderLock.Dispose();
        _applyOnlyCustomizationToCharacterProviderLock.Dispose();

        _revertProvider.Dispose();
        _revertCharacterProvider.Dispose();
        _revertProviderLock.Dispose();
        _revertCharacterProviderLock.Dispose();
        _unlockNameProvider.Dispose();
        _unlockProvider.Dispose();
        _revertToAutomationProvider.Dispose();
        _revertToAutomationCharacterProvider.Dispose();

        _stateChangedEvent.Unsubscribe(OnStateChanged);
        _stateChangedProvider.Dispose();
        _gPose.Unsubscribe(OnGPoseChanged);
        _gPoseChangedProvider.Dispose();
    }

    private IEnumerable<ActorIdentifier> FindActors(string actorName)
    {
        if (actorName.Length == 0 || !ByteString.FromString(actorName, out var byteString))
            return Array.Empty<ActorIdentifier>();

        _objects.Update();
        return _objects.Where(i => i.Key is { IsValid: true, Type: IdentifierType.Player } && i.Key.PlayerName == byteString)
            .Select(i => i.Key);
    }

    private IEnumerable<ActorIdentifier> FindActorsRevert(string actorName)
    {
        if (actorName.Length == 0 || !ByteString.FromString(actorName, out var byteString))
            yield break;

        _objects.Update();
        foreach (var id in _objects.Where(i => i.Key is { IsValid: true, Type: IdentifierType.Player } && i.Key.PlayerName == byteString)
                     .Select(i => i.Key))
            yield return id;

        foreach (var id in _stateManager.Keys.Where(s => s.Type is IdentifierType.Player && s.PlayerName == byteString))
            yield return id;
    }

    private IEnumerable<ActorIdentifier> FindActors(Character? character)
    {
        var id = _actors.FromObject(character, true, true, false);
        if (!id.IsValid)
            yield break;

        yield return id;
    }
}
