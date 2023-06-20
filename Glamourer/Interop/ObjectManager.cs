using System;
using System.Collections;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Penumbra.GameData.Actors;

namespace Glamourer.Interop;

public class ObjectManager : IReadOnlyDictionary<ActorIdentifier, ActorData>
{
    private readonly Framework    _framework;
    private readonly ClientState  _clientState;
    private readonly ObjectTable  _objects;
    private readonly ActorService _actors;

    public ObjectManager(Framework framework, ClientState clientState, ObjectTable objects, ActorService actors)
    {
        _framework   = framework;
        _clientState = clientState;
        _objects     = objects;
        _actors      = actors;
    }

    public DateTime LastUpdate { get; private set; }

    public bool   IsInGPose { get; private set; }
    public ushort World     { get; private set; }

    private readonly Dictionary<ActorIdentifier, ActorData> _identifiers = new(200);

    public IReadOnlyDictionary<ActorIdentifier, ActorData> Identifiers
        => _identifiers;

    public void Update()
    {
        var lastUpdate = _framework.LastUpdate;
        if (lastUpdate <= LastUpdate)
            return;

        LastUpdate = lastUpdate;
        World      = (ushort)(_clientState.LocalPlayer?.CurrentWorld.Id ?? 0u);
        _identifiers.Clear();

        for (var i = 0; i < (int)ScreenActor.CutsceneStart; ++i)
        {
            Actor character = _objects.GetObjectAddress(i);
            if (character.Identifier(_actors.AwaitedService, out var identifier))
                HandleIdentifier(identifier, character);
        }

        for (var i = (int)ScreenActor.CutsceneStart; i < (int)ScreenActor.CutsceneEnd; ++i)
        {
            Actor character = _objects.GetObjectAddress(i);
            if (!character.Valid)
                break;

            HandleIdentifier(character.GetIdentifier(_actors.AwaitedService), character);
        }

        void AddSpecial(ScreenActor idx, string label)
        {
            Actor actor = _objects.GetObjectAddress((int)idx);
            if (actor.Identifier(_actors.AwaitedService, out var ident))
            {
                var data = new ActorData(actor, label);
                _identifiers.Add(ident, data);
            }
        }

        AddSpecial(ScreenActor.CharacterScreen, "Character Screen Actor");
        AddSpecial(ScreenActor.ExamineScreen,   "Examine Screen Actor");
        AddSpecial(ScreenActor.FittingRoom,     "Fitting Room Actor");
        AddSpecial(ScreenActor.DyePreview,      "Dye Preview Actor");
        AddSpecial(ScreenActor.Portrait,        "Portrait Actor");
        AddSpecial(ScreenActor.Card6,           "Card Actor 6");
        AddSpecial(ScreenActor.Card7,           "Card Actor 7");
        AddSpecial(ScreenActor.Card8,           "Card Actor 8");

        for (var i = (int)ScreenActor.ScreenEnd; i < _objects.Length; ++i)
        {
            Actor character = _objects.GetObjectAddress(i);
            if (character.Identifier(_actors.AwaitedService, out var identifier))
                HandleIdentifier(identifier, character);
        }

        var gPose = GPosePlayer;
        IsInGPose = gPose.Utf8Name.Length > 0;
    }

    private void HandleIdentifier(ActorIdentifier identifier, Actor character)
    {
        if (!character.Model || !identifier.IsValid)
            return;

        if (!_identifiers.TryGetValue(identifier, out var data))
        {
            data                     = new ActorData(character, identifier.ToString());
            _identifiers[identifier] = data;
        }
        else
        {
            data.Objects.Add(character);
        }
    }

    public Actor GPosePlayer
        => _objects.GetObjectAddress((int)ScreenActor.GPosePlayer);

    public Actor Player
        => _objects.GetObjectAddress(0);

    public (ActorIdentifier Identifier, ActorData Data) PlayerData
    {
        get
        {
            Update();
            return Player.Identifier(_actors.AwaitedService, out var ident) && _identifiers.TryGetValue(ident, out var data)
                ? (ident, data)
                : (ActorIdentifier.Invalid, ActorData.Invalid);
        }
    }

    public IEnumerator<KeyValuePair<ActorIdentifier, ActorData>> GetEnumerator()
        => Identifiers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => Identifiers.Count;

    public bool ContainsKey(ActorIdentifier key)
        => Identifiers.ContainsKey(key);

    public bool TryGetValue(ActorIdentifier key, out ActorData value)
        => Identifiers.TryGetValue(key, out value);

    public ActorData this[ActorIdentifier key]
        => Identifiers[key];

    public IEnumerable<ActorIdentifier> Keys
        => Identifiers.Keys;

    public IEnumerable<ActorData> Values
        => Identifiers.Values;
}
