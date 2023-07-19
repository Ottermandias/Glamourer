using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Events;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelRevert          = "Glamourer.Revert";
    public const string LabelRevertCharacter = "Glamourer.RevertCharacter";

    private readonly ActionProvider<string>     _revertProvider;
    private readonly ActionProvider<Character?> _revertCharacterProvider;

    public static ActionSubscriber<string> RevertSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevert);

    public static ActionSubscriber<Character?> RevertCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelRevertCharacter);

    public void Revert(string characterName)
        => Revert(FindActors(characterName));

    public void RevertCharacter(Character? character)
        => Revert(FindActors(character));

    private void Revert(IEnumerable<ActorIdentifier> actors)
    {
        foreach (var id in actors)
        {
            if (_stateManager.TryGetValue(id, out var state))
                _stateManager.ResetState(state, StateChanged.Source.Ipc, 0xDEADBEEF);
        }
    }
}
