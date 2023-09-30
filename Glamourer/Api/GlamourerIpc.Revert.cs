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
    public const string LabelRevertToAutomation  = "Glamourer.RevertToAutomation";

    private readonly ActionProvider<string>     _revertProvider;
    private readonly ActionProvider<Character?> _revertCharacterProvider;

    private readonly ActionProvider<string, uint>     _revertProviderLock;
    private readonly ActionProvider<Character?, uint> _revertCharacterProviderLock;

    private readonly FuncProvider<Character?, uint, bool> _revertToAutomationProvider;

    private readonly FuncProvider<Character?, uint, bool> _unlockProvider;

    public static ActionSubscriber<string> RevertSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevert);

    public static ActionSubscriber<Character?> RevertCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevertCharacter);

    public static FuncSubscriber<Character?, uint, bool> UnlockSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelUnlock);

    public static FuncSubscriber<Character?, uint, bool> RevertToAutomationSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevertToAutomation);

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

    public bool RevertToAutomation(Character? character, uint lockCode)
        => RevertToAutomation(FindActors(character), lockCode);

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

    private bool RevertToAutomation(IEnumerable<ActorIdentifier> actors, uint lockCode)
    {
        var ret = false;
        foreach (var id in actors)
        {
            if (_stateManager.TryGetValue(id, out var state))
            {
                ret |= state.Unlock(lockCode);
                if (_objects.TryGetValue(id, out var data))
                    foreach (var obj in data.Objects)
                    {
                        _autoDesignApplier.ReapplyAutomation(obj, state.Identifier, state);
                        _stateManager.ReapplyState(obj);
                    }
            }
        }

        return ret;
    }
}
