using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.State;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.String;

namespace Glamourer.Api;

public sealed partial class GlamourerIpc : IDisposable
{
    public const int CurrentApiVersionMajor = 0;
    public const int CurrentApiVersionMinor = 5;

    private readonly StateManager      _stateManager;
    private readonly ObjectManager     _objects;
    private readonly ActorManager      _actors;
    private readonly DesignConverter   _designConverter;
    private readonly AutoDesignApplier _autoDesignApplier;
    private readonly DesignManager     _designManager;
    private readonly ItemManager       _items;
    private readonly Configuration     _config;

    public GlamourerIpc(DalamudPluginInterface pi, StateManager stateManager, ObjectManager objects, ActorManager actors,
        DesignConverter designConverter, StateChanged stateChangedEvent, GPoseService gPose, AutoDesignApplier autoDesignApplier,
        DesignManager designManager, ItemManager items, Configuration config)
    {
        _stateManager        = stateManager;
        _objects             = objects;
        _actors              = actors;
        _designConverter     = designConverter;
        _autoDesignApplier   = autoDesignApplier;
        _items               = items;
        _config              = config;
        _gPose               = gPose;
        _stateChangedEvent   = stateChangedEvent;
        _designManager       = designManager;
        _apiVersionProvider  = new FuncProvider<int>(pi, LabelApiVersion, ApiVersion);
        _apiVersionsProvider = new FuncProvider<(int Major, int Minor)>(pi, LabelApiVersions, ApiVersions);

        _getAllCustomizationProvider = new FuncProvider<string, string?>(pi, LabelGetAllCustomization, GetAllCustomization);
        _getAllCustomizationFromCharacterProvider =
            new FuncProvider<Character?, string?>(pi, LabelGetAllCustomizationFromCharacter, GetAllCustomizationFromCharacter);
        _getAllCustomizationLockedProvider = new FuncProvider<string, uint, string?>(pi, LabelGetAllCustomizationLocked, GetAllCustomization);
        _getAllCustomizationFromLockedCharacterProvider =
            new FuncProvider<Character?, uint, string?>(pi, LabelGetAllCustomizationFromLockedCharacter, GetAllCustomizationFromCharacter);

        _applyAllProvider                = new ActionProvider<string, string>(pi, LabelApplyAll,     ApplyAll);
        _applyAllOnceProvider            = new ActionProvider<string, string>(pi, LabelApplyAllOnce, ApplyAllOnce);
        _applyAllToCharacterProvider     = new ActionProvider<string, Character?>(pi, LabelApplyAllToCharacter,     ApplyAllToCharacter);
        _applyAllOnceToCharacterProvider = new ActionProvider<string, Character?>(pi, LabelApplyAllOnceToCharacter, ApplyAllOnceToCharacter);
        _applyOnlyEquipmentProvider      = new ActionProvider<string, string>(pi, LabelApplyOnlyEquipment, ApplyOnlyEquipment);
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

        _applyByGuidProvider            = new ActionProvider<Guid, string>(pi, LabelApplyByGuid,     ApplyByGuid);
        _applyByGuidOnceProvider        = new ActionProvider<Guid, string>(pi, LabelApplyByGuidOnce, ApplyByGuidOnce);
        _applyByGuidToCharacterProvider = new ActionProvider<Guid, Character?>(pi, LabelApplyByGuidToCharacter, ApplyByGuidToCharacter);
        _applyByGuidOnceToCharacterProvider =
            new ActionProvider<Guid, Character?>(pi, LabelApplyByGuidOnceToCharacter, ApplyByGuidOnceToCharacter);

        _revertProvider              = new ActionProvider<string>(pi, LabelRevert, Revert);
        _revertCharacterProvider     = new ActionProvider<Character?>(pi, LabelRevertCharacter, RevertCharacter);
        _revertProviderLock          = new ActionProvider<string, uint>(pi, LabelRevertLock, RevertLock);
        _revertCharacterProviderLock = new ActionProvider<Character?, uint>(pi, LabelRevertCharacterLock, RevertCharacterLock);
        _unlockNameProvider          = new FuncProvider<string, uint, bool>(pi, LabelUnlockName, Unlock);
        _unlockProvider              = new FuncProvider<Character?, uint, bool>(pi, LabelUnlock, Unlock);
        _unlockAllProvider           = new FuncProvider<uint, int>(pi, LabelUnlockAll, UnlockAll);
        _revertToAutomationProvider  = new FuncProvider<string, uint, bool>(pi, LabelRevertToAutomation, RevertToAutomation);
        _revertToAutomationCharacterProvider =
            new FuncProvider<Character?, uint, bool>(pi, LabelRevertToAutomationCharacter, RevertToAutomation);

        _stateChangedProvider = new EventProvider<StateChanged.Type, nint, Lazy<string>>(pi, LabelStateChanged);
        _gPoseChangedProvider = new EventProvider<bool>(pi, LabelGPoseChanged);

        _setItemProvider = new FuncProvider<Character?, byte, ulong, byte, uint, int>(pi, LabelSetItem,
            (idx, slot, item, stain, key) => (int)SetItem(idx, (EquipSlot)slot, item, stain, key, false));
        _setItemOnceProvider = new FuncProvider<Character?, byte, ulong, byte, uint, int>(pi, LabelSetItemOnce,
            (idx, slot, item, stain, key) => (int)SetItem(idx, (EquipSlot)slot, item, stain, key, true));

        _setItemByActorNameProvider = new FuncProvider<string, byte, ulong, byte, uint, int>(pi, LabelSetItemByActorName,
            (name, slot, item, stain, key) => (int)SetItemByActorName(name, (EquipSlot)slot, item, stain, key, false));
        _setItemOnceByActorNameProvider = new FuncProvider<string, byte, ulong, byte, uint, int>(pi, LabelSetItemOnceByActorName,
            (name, slot, item, stain, key) => (int)SetItemByActorName(name, (EquipSlot)slot, item, stain, key, true));

        _stateChangedEvent.Subscribe(OnStateChanged, StateChanged.Priority.GlamourerIpc);
        _gPose.Subscribe(OnGPoseChanged, GPoseService.Priority.GlamourerIpc);

        _getDesignListProvider = new FuncProvider<(string Name, Guid Identifier)[]>(pi, LabelGetDesignList, GetDesignList);
    }

    public void Dispose()
    {
        _apiVersionProvider.Dispose();
        _apiVersionsProvider.Dispose();

        _getAllCustomizationProvider.Dispose();
        _getAllCustomizationLockedProvider.Dispose();
        _getAllCustomizationFromCharacterProvider.Dispose();
        _getAllCustomizationFromLockedCharacterProvider.Dispose();

        _applyAllProvider.Dispose();
        _applyAllOnceProvider.Dispose();
        _applyAllToCharacterProvider.Dispose();
        _applyAllOnceToCharacterProvider.Dispose();
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

        _applyByGuidProvider.Dispose();
        _applyByGuidOnceProvider.Dispose();
        _applyByGuidToCharacterProvider.Dispose();
        _applyByGuidOnceToCharacterProvider.Dispose();

        _revertProvider.Dispose();
        _revertCharacterProvider.Dispose();
        _revertProviderLock.Dispose();
        _revertCharacterProviderLock.Dispose();
        _unlockNameProvider.Dispose();
        _unlockProvider.Dispose();
        _unlockAllProvider.Dispose();
        _revertToAutomationProvider.Dispose();
        _revertToAutomationCharacterProvider.Dispose();

        _stateChangedEvent.Unsubscribe(OnStateChanged);
        _stateChangedProvider.Dispose();
        _gPose.Unsubscribe(OnGPoseChanged);
        _gPoseChangedProvider.Dispose();

        _getDesignListProvider.Dispose();

        _setItemProvider.Dispose();
        _setItemOnceProvider.Dispose();
        _setItemByActorNameProvider.Dispose();
        _setItemOnceByActorNameProvider.Dispose();
    }

    private IEnumerable<ActorIdentifier> FindActors(string actorName)
    {
        if (actorName.Length == 0 || !ByteString.FromString(actorName, out var byteString))
            return [];

        _objects.Update();
        return _objects.Keys.Where(i => i is { IsValid: true, Type: IdentifierType.Player } && i.PlayerName == byteString);
    }

    private IEnumerable<ActorIdentifier> FindActorsRevert(string actorName)
    {
        if (actorName.Length == 0 || !ByteString.FromString(actorName, out var byteString))
            yield break;

        _objects.Update();
        foreach (var id in _objects.Keys.Where(i => i is { IsValid: true, Type: IdentifierType.Player } && i.PlayerName == byteString)
                     .Select(i => i))
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
