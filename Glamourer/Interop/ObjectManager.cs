using System;
using System.Collections;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Penumbra.GameData.Actors;

namespace Glamourer.Interop;

public class ObjectManager : IReadOnlyDictionary<ActorIdentifier, ObjectManager.ActorData>
{
    public readonly struct ActorData
    {
        public readonly List<Actor> Objects;
        public readonly string      Label;

        public bool Valid
            => Objects.Count > 0;

        public ActorData(Actor actor, string label)
        {
            Objects = new List<Actor> { actor };
            Label   = label;
        }

        public static readonly ActorData Invalid = new(false);

        private ActorData(bool _)
        {
            Objects = new List<Actor>(0);
            Label   = string.Empty;
        }
    }

    public DateTime LastUpdate { get; private set; }

    public bool   IsInGPose { get; private set; }
    public ushort World     { get; private set; }

    private readonly Dictionary<ActorIdentifier, ActorData> _identifiers = new(200);

    private void HandleIdentifier(ActorIdentifier identifier, Actor character)
    {
        if (!character.DrawObject || !identifier.IsValid)
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

    private readonly Framework   _framework;
    private readonly ClientState _clientState;
    private readonly ObjectTable _objects;

    public ObjectManager(Framework framework, ClientState clientState, ObjectTable objects)
    {
        _framework   = framework;
        _clientState = clientState;
        _objects     = objects;
    }

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
            if (character.Identifier(out var identifier))
                HandleIdentifier(identifier, character);
        }

        for (var i = (int)ScreenActor.CutsceneStart; i < (int)ScreenActor.CutsceneEnd; ++i)
        {
            Actor character = _objects.GetObjectAddress(i);
            if (!character.Identifier(out var identifier))
                break;

            HandleIdentifier(identifier, character);
        }

        void AddSpecial(ScreenActor idx, string label)
        {
            Actor actor = _objects.GetObjectAddress((int)idx);
            if (actor.Identifier(out var ident))
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

        for (var i = (int)ScreenActor.ScreenEnd; i < Dalamud.Objects.Length; ++i)
        {
            Actor character = _objects.GetObjectAddress(i);
            if (character.Identifier(out var identifier))
                HandleIdentifier(identifier, character);
        }

        var gPose = GPosePlayer;
        IsInGPose = gPose && gPose.Utf8Name.Length > 0;
    }

    public Actor GPosePlayer
        => _objects.GetObjectAddress((int)ScreenActor.GPosePlayer);

    public Actor Player
        => _objects.GetObjectAddress(0);

    public IEnumerator<KeyValuePair<ActorIdentifier, ActorData>> GetEnumerator()
        => _identifiers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _identifiers.Count;

    public bool ContainsKey(ActorIdentifier key)
        => _identifiers.ContainsKey(key);

    public bool TryGetValue(ActorIdentifier key, out ActorData value)
        => _identifiers.TryGetValue(key, out value);

    public ActorData this[ActorIdentifier key]
        => _identifiers[key];

    public IEnumerable<ActorIdentifier> Keys
        => _identifiers.Keys;

    public IEnumerable<ActorData> Values
        => _identifiers.Values;
}
