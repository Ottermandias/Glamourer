using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.State;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;
using Penumbra.String;

namespace Glamourer.Api;

public partial class GlamourerIpc : IDisposable
{
    public const int CurrentApiVersionMajor = 0;
    public const int CurrentApiVersionMinor = 2;

    private readonly StateManager    _stateManager;
    private readonly ObjectManager   _objects;
    private readonly ActorService    _actors;
    private readonly DesignConverter _designConverter;

    public GlamourerIpc(DalamudPluginInterface pi, StateManager stateManager, ObjectManager objects, ActorService actors,
        DesignConverter designConverter, StateChanged stateChangedEvent)
    {
        _stateManager        = stateManager;
        _objects             = objects;
        _actors              = actors;
        _designConverter     = designConverter;
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

        _revertProvider          = new ActionProvider<string>(pi, LabelRevert, Revert);
        _revertCharacterProvider = new ActionProvider<Character?>(pi, LabelRevertCharacter, RevertCharacter);

        _stateChangedProvider = new EventProvider<StateChanged.Type, nint, Lazy<string>>(pi, LabelStateChanged);

        _stateChangedEvent.Subscribe(OnStateChanged, StateChanged.Priority.GlamourerIpc);
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
        _revertProvider.Dispose();
        _revertCharacterProvider.Dispose();

        _stateChangedEvent.Unsubscribe(OnStateChanged);
        _stateChangedProvider.Dispose();
    }

    private IEnumerable<ActorIdentifier> FindActors(string actorName)
    {
        if (actorName.Length == 0 || !ByteString.FromString(actorName, out var byteString))
            return Array.Empty<ActorIdentifier>();

        _objects.Update();
        return _objects.Where(i => i.Key is { IsValid: true, Type: IdentifierType.Player } && i.Key.PlayerName == byteString)
            .Select(i => i.Key);
    }

    private IEnumerable<ActorIdentifier> FindActors(Character? character)
    {
        var id = _actors.AwaitedService.FromObject(character, true, true, false);
        if (!id.IsValid)
            yield break;

        yield return id;
    }
}
