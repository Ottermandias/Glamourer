using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Events;
using Glamourer.Interop.Structs;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelApplyByGuidAll = "Glamourer.ApplyByGuidAll";
    public const string LabelApplyByGuidAllToCharacter = "Glamourer.ApplyByGuidAllToCharacter";

    private readonly ActionProvider<Guid, string> _applyByGuidAllProvider;
    private readonly ActionProvider<Guid, Character?> _applyByGuidAllToCharacterProvider;

    public static ActionSubscriber<Guid, string> ApplyByGuidAllSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyByGuidAll);

    public static ActionSubscriber<Guid, Character?> ApplyByGuidAllToCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyByGuidAllToCharacter);

    public void ApplyByGuidAll(Guid GUID, string characterName)
        => ApplyDesignByGuid(GUID, FindActors(characterName), 0);

    public void ApplyByGuidAllToCharacter(Guid GUID, Character? character)
        => ApplyDesignByGuid(GUID, FindActors(character), 0);

    private void ApplyDesignByGuid(Guid GUID, IEnumerable<ActorIdentifier> actors, uint lockCode)
    {
        var design = _designManager.Designs.FirstOrDefault(x => x.Identifier == GUID);
        if (design == null)
            return;

        var hasModelId = true;
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
}
