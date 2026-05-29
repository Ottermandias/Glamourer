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

public sealed class ActorSelection : IUiService, IDisposable
{
    private readonly StateManager       _manager;
    private readonly ActorObjectManager _objects;
    private readonly ICondition         _conditions;
    private readonly UiConfig           _config;
    private readonly NavigationService  _navigation;

    public ActorSelection(StateManager manager, ActorObjectManager objects, ICondition conditions, UiConfig config,
        NavigationService navigation)
    {
        _manager          =  manager;
        _objects          =  objects;
        _conditions       =  conditions;
        _config           =  config;
        _navigation       =  navigation;
        _navigation.Actor += Select;
    }

    private static readonly StringU8 NoSelection  = new("No Actor Selected"u8);
    private static readonly StringU8 NotAvailable = new("N/A"u8);

    public ActorIdentifier Identifier    { get; private set; }
    public ActorState?     State         { get; private set; }
    public StringU8        ActorName     { get; private set; } = NoSelection;
    public StringU8        IncognitoName { get; private set; } = NoSelection;
    public ActorData       Data          { get; private set; } = ActorData.Invalid;
    public StringU8        ShortName     { get; private set; } = NotAvailable;
    public Actor           Actor         { get; private set; } = Actor.Null;
    public bool            LockedRedraw  { get; private set; } = false;

    public void Select(ActorState? state)
    {
        if (state is null)
        {
            Identifier    = ActorIdentifier.Invalid;
            ActorName     = NoSelection;
            IncognitoName = NoSelection;
            ShortName     = NotAvailable;
        }
        else
        {
            Identifier = state.Identifier;
            var label = Identifier.ToString();
            ActorName     = new StringU8(label);
            IncognitoName = new StringU8(Identifier.Incognito(label));
            ShortName = Identifier.Type switch
            {
                IdentifierType.Player   => IncognitoName,
                IdentifierType.Owned    => new StringU8($"Owned NPC #{Identifier.Index.Index}"),
                IdentifierType.Special  => new StringU8($"Screen Actor #{Identifier.Index.Index}"),
                IdentifierType.Npc      => new StringU8($"NPC #{Identifier.Index.Index}"),
                IdentifierType.Retainer => IncognitoName,
                _                       => NotAvailable,
            };
        }

        State                 = state;
        _config.SelectedActor = Identifier;
    }

    public void Select(ActorIdentifier identifier, ActorData data)
    {
        Identifier            = identifier.CreatePermanent();
        _config.SelectedActor = Identifier;
        if (Identifier.IsValid)
        {
            ActorName     = new StringU8(data.Label);
            IncognitoName = new StringU8(Identifier.Incognito(data.Label));
            // Try to get an existing state, or try to create one if possible.
            State = _manager.TryGetValue(Identifier, out var s) || data.Valid && _manager.GetOrCreate(Identifier, data.Objects[0], out s)
                ? s
                : null;
            ShortName = Identifier.Type switch
            {
                IdentifierType.Player   => IncognitoName,
                IdentifierType.Owned    => new StringU8($"Owned NPC #{Identifier.Index.Index}"),
                IdentifierType.Special  => new StringU8($"Screen Actor #{Identifier.Index.Index}"),
                IdentifierType.Npc      => new StringU8($"NPC #{Identifier.Index.Index}"),
                IdentifierType.Retainer => IncognitoName,
                _                       => NotAvailable,
            };
        }
        else
        {
            ActorName     = NoSelection;
            IncognitoName = NoSelection;
            ShortName     = NotAvailable;
        }
    }

    public void Update()
    {
        if (Identifier.IsValid)
        {
            if (_objects.TryGetValue(Identifier, out var data))
            {
                Data  = data;
                Actor = Data.Objects[0];
            }
            else
            {
                Data  = ActorData.Invalid;
                Actor = Actor.Null;
            }

            LockedRedraw = Identifier.Type is IdentifierType.Special
             || _objects.IsInLobby
             || _conditions[ConditionFlag.OccupiedInCutSceneEvent];
        }
        else
        {
            Data         = ActorData.Invalid;
            Actor        = Actor.Null;
            LockedRedraw = false;
        }
    }

    public void Dispose()
        => _navigation.Actor -= Select;
}
