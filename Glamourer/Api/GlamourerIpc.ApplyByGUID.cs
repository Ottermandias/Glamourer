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
    public const string LabelApplyByGuid = "Glamourer.ApplyByGuid";
    public const string LabelApplyByGuidToCharacter = "Glamourer.ApplyByGuidToCharacter";

    private readonly ActionProvider<Guid, string> _applyByGuidProvider;
    private readonly ActionProvider<Guid, Character?> _applyByGuidToCharacterProvider;

    public static ActionSubscriber<Guid, string> ApplyByGuidSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyByGuid);

    public static ActionSubscriber<Guid, Character?> ApplyByGuidToCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyByGuidToCharacter);

    public void ApplyByGuid(Guid Identifier, string characterName)
        => ApplyDesignByGuid(Identifier, FindActors(characterName), 0);

    public void ApplyByGuidToCharacter(Guid Identifier, Character? character)
        => ApplyDesignByGuid(Identifier, FindActors(character), 0);

    private void ApplyDesignByGuid(Guid Identifier, IEnumerable<ActorIdentifier> actors, uint lockCode)
    {
        var design = _designManager.Designs.FirstOrDefault(x => x.Identifier == Identifier);
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
