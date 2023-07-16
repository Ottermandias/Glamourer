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
    private readonly Framework     _framework;
    private readonly ClientState   _clientState;
    private readonly ObjectTable   _objects;
    private readonly ActorService  _actors;
    private readonly TargetManager _targets;

    public ObjectManager(Framework framework, ClientState clientState, ObjectTable objects, ActorService actors, TargetManager targets)
    {
        _framework   = framework;
        _clientState = clientState;
        _objects     = objects;
        _actors      = actors;
        _targets     = targets;
    }

    public DateTime LastUpdate { get; private set; }

    public bool   IsInGPose { get; private set; }
    public ushort World     { get; private set; }

    private readonly Dictionary<ActorIdentifier, ActorData> _identifiers         = new(200);
    private readonly Dictionary<ActorIdentifier, ActorData> _allWorldIdentifiers = new(200);

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
        _allWorldIdentifiers.Clear();

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

        if (identifier.Type is not (IdentifierType.Player or IdentifierType.Owned))
            return;

        var allWorld = _actors.AwaitedService.CreateIndividualUnchecked(identifier.Type, identifier.PlayerName, ushort.MaxValue,
            identifier.Kind,
            identifier.DataId);

        if (!_allWorldIdentifiers.TryGetValue(allWorld, out var allWorldData))
        {
            allWorldData                   = new ActorData(character, allWorld.ToString());
            _allWorldIdentifiers[allWorld] = allWorldData;
        }
        else
        {
            allWorldData.Objects.Add(character);
        }
    }

    public Actor GPosePlayer
        => _objects.GetObjectAddress((int)ScreenActor.GPosePlayer);

    public Actor Player
        => _objects.GetObjectAddress(0);

    public Actor Target
        => _targets.Target?.Address ?? nint.Zero;

    public Actor Focus
        => _targets.FocusTarget?.Address ?? nint.Zero;

    public Actor MouseOver
        => _targets.MouseOverTarget?.Address ?? nint.Zero;

    public (ActorIdentifier Identifier, ActorData Data) PlayerData
    {
        get
        {
            Update();
            return Player.Identifier(_actors.AwaitedService, out var ident) && _identifiers.TryGetValue(ident, out var data)
                ? (ident, data)
                : (ident, ActorData.Invalid);
        }
    }

    public (ActorIdentifier Identifier, ActorData Data) TargetData
    {
        get
        {
            Update();
            return Target.Identifier(_actors.AwaitedService, out var ident) && _identifiers.TryGetValue(ident, out var data)
                ? (ident, data)
                : (ident, ActorData.Invalid);
        }
    }

    public IEnumerator<KeyValuePair<ActorIdentifier, ActorData>> GetEnumerator()
        => Identifiers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => Identifiers.Count;

    /// <summary> Also handles All Worlds players. </summary>
    public bool ContainsKey(ActorIdentifier key)
        => Identifiers.ContainsKey(key) || _allWorldIdentifiers.ContainsKey(key);

    public bool TryGetValue(ActorIdentifier key, out ActorData value)
        => Identifiers.TryGetValue(key, out value);

    public bool TryGetValueAllWorld(ActorIdentifier key, out ActorData value)
        => _allWorldIdentifiers.TryGetValue(key, out value);

    public ActorData this[ActorIdentifier key]
        => Identifiers[key];

    public IEnumerable<ActorIdentifier> Keys
        => Identifiers.Keys;

    public IEnumerable<ActorData> Values
        => Identifiers.Values;

    public bool GetName(string lowerName, out Actor actor)
    {
        (actor, var ret) = lowerName switch
        {
            ""          => (Actor.Null, true),
            "<me>"      => (Player, true),
            "self"      => (Player, true),
            "<t>"       => (Target, true),
            "target"    => (Target, true),
            "<f>"       => (Focus, true),
            "focus"     => (Focus, true),
            "<mo>"      => (MouseOver, true),
            "mouseover" => (MouseOver, true),
            _           => (Actor.Null, false),
        };
        return ret;
    }
}
