using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.State;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelRevert                      = "Glamourer.Revert";
    public const string LabelRevertCharacter             = "Glamourer.RevertCharacter";
    public const string LabelRevertLock                  = "Glamourer.RevertLock";
    public const string LabelRevertCharacterLock         = "Glamourer.RevertCharacterLock";
    public const string LabelRevertToAutomation          = "Glamourer.RevertToAutomation";
    public const string LabelRevertToAutomationCharacter = "Glamourer.RevertToAutomationCharacter";
    public const string LabelUnlock                      = "Glamourer.Unlock";
    public const string LabelUnlockName                  = "Glamourer.UnlockName";
    public const string LabelUnlockAll                   = "Glamourer.UnlockAll";

    private readonly ActionProvider<string>     _revertProvider;
    private readonly ActionProvider<Character?> _revertCharacterProvider;

    private readonly ActionProvider<string, uint>     _revertProviderLock;
    private readonly ActionProvider<Character?, uint> _revertCharacterProviderLock;

    private readonly FuncProvider<string, uint, bool>     _revertToAutomationProvider;
    private readonly FuncProvider<Character?, uint, bool> _revertToAutomationCharacterProvider;

    private readonly FuncProvider<string, uint, bool>     _unlockNameProvider;
    private readonly FuncProvider<Character?, uint, bool> _unlockProvider;

    private readonly FuncProvider<uint, int>     _unlockAllProvider;

    public static ActionSubscriber<string> RevertSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevert);

    public static ActionSubscriber<Character?> RevertCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevertCharacter);

    public static ActionSubscriber<string> RevertLockSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevertLock);

    public static ActionSubscriber<Character?> RevertCharacterLockSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevertCharacterLock);

    public static FuncSubscriber<string, uint, bool> UnlockNameSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelUnlockName);

    public static FuncSubscriber<Character?, uint, bool> UnlockSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelUnlock);

    public static FuncSubscriber<uint, int> UnlockAllSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelUnlockAll);

    public static FuncSubscriber<string, uint, bool> RevertToAutomationSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevertToAutomation);

    public static FuncSubscriber<Character?, uint, bool> RevertToAutomationCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevertToAutomationCharacter);

    public void Revert(string characterName)
        => Revert(FindActorsRevert(characterName), 0);

    public void RevertCharacter(Character? character)
        => Revert(FindActors(character), 0);

    public void RevertLock(string characterName, uint lockCode)
        => Revert(FindActorsRevert(characterName), lockCode);

    public void RevertCharacterLock(Character? character, uint lockCode)
        => Revert(FindActors(character), lockCode);

    public bool Unlock(string characterName, uint lockCode)
        => Unlock(FindActorsRevert(characterName), lockCode);

    public bool Unlock(Character? character, uint lockCode)
        => Unlock(FindActors(character), lockCode);

    public int UnlockAll(uint lockCode)
    {
        var count = 0;
        foreach (var state in _stateManager.Values)
            if (state.Unlock(lockCode))
                ++count;
        return count;
    }

    public bool RevertToAutomation(string characterName, uint lockCode)
        => RevertToAutomation(FindActorsRevert(characterName), lockCode);

    public bool RevertToAutomation(Character? character, uint lockCode)
        => RevertToAutomation(FindActors(character), lockCode);

    private void Revert(IEnumerable<ActorIdentifier> actors, uint lockCode)
    {
        foreach (var id in actors)
        {
            if (_stateManager.TryGetValue(id, out var state))
                _stateManager.ResetState(state, StateSource.IpcFixed, lockCode);
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
                        _stateManager.ReapplyState(obj, StateSource.IpcManual);
                    }
            }
        }

        return ret;
    }
}
