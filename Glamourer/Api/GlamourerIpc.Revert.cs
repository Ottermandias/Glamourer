using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Events;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelRevert              = "Glamourer.Revert";
    public const string LabelRevertCharacter     = "Glamourer.RevertCharacter";
    public const string LabelRevertLock          = "Glamourer.RevertLock";
    public const string LabelRevertCharacterLock = "Glamourer.RevertCharacterLock";
    public const string LabelUnlock              = "Glamourer.Unlock";

    private readonly ActionProvider<string>     _revertProvider;
    private readonly ActionProvider<Character?> _revertCharacterProvider;

    private readonly ActionProvider<string, uint>     _revertProviderLock;
    private readonly ActionProvider<Character?, uint> _revertCharacterProviderLock;

    private readonly FuncProvider<Character?, uint, bool> _unlockProvider;

    public static ActionSubscriber<string> RevertSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevert);

    public static ActionSubscriber<Character?> RevertCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevertCharacter);

    public void Revert(string characterName)
        => Revert(FindActors(characterName), 0);

    public void RevertCharacter(Character? character)
        => Revert(FindActors(character), 0);

    public void RevertLock(string characterName, uint lockCode)
        => Revert(FindActors(characterName), lockCode);

    public void RevertCharacterLock(Character? character, uint lockCode)
        => Revert(FindActors(character), lockCode);

    public bool Unlock(Character? character, uint lockCode)
        => Unlock(FindActors(character), lockCode);

    private void Revert(IEnumerable<ActorIdentifier> actors, uint lockCode)
    {
        foreach (var id in actors)
        {
            if (_stateManager.TryGetValue(id, out var state))
                _stateManager.ResetState(state, StateChanged.Source.Ipc, lockCode);
        }
    }

    private bool Unlock(IEnumerable<ActorIdentifier> actors, uint lockCode)
    {
        var ret = false;
        foreach (var id in actors)
        {
            if (_stateManager.TryGetValue(id, out var state))
                ret |= state.Unlock(lockCode);
        }

        return ret;
    }
}
