using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Glamourer.Config;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class ActorSelection(StateManager manager, ActorObjectManager objects, ICondition conditions, UiConfig config) : IUiService
{
    private static readonly StringU8 NoSelection = new("No Actor Selected"u8);

    public ActorIdentifier Identifier    { get; private set; }
    public ActorState?     State         { get; private set; }
    public StringU8        ActorName     { get; private set; } = NoSelection;
    public StringU8        IncognitoName { get; private set; } = NoSelection;
    public ActorData       Data          { get; private set; } = ActorData.Invalid;
    public Actor           Actor         { get; private set; } = Actor.Null;
    public bool            LockedRedraw  { get; private set; } = false;

    public void Select(ActorIdentifier identifier, ActorData data)
    {
        Identifier           = identifier.CreatePermanent();
        config.SelectedActor = Identifier;
        if (Identifier.IsValid)
        {
            ActorName     = new StringU8(data.Label);
            IncognitoName = new StringU8(Identifier.Incognito(data.Label));
            State         = data.Valid && manager.GetOrCreate(Identifier, data.Objects[0], out var s) ? s : null;
        }
        else
        {
            ActorName     = NoSelection;
            IncognitoName = NoSelection;
        }
    }

    public void Update()
    {
        if (Identifier.IsValid)
        {
            if (objects.TryGetValue(Identifier, out var data))
            {
                Data  = data;
                Actor = Data.Objects[0];
            }
            else
            {
                Data  = ActorData.Invalid;
                Actor = Actor.Null;
            }

            LockedRedraw = Identifier.Type is IdentifierType.Special || objects.IsInLobby || conditions[ConditionFlag.OccupiedInCutSceneEvent];
        }
        else
        {
            Data         = ActorData.Invalid;
            Actor        = Actor.Null;
            LockedRedraw = false;
        }
    }
}
