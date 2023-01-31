using System.Collections;
using Glamourer.Interop;
using Penumbra.GameData.Actors;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Glamourer.Designs;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public sealed partial class ActiveDesign
{
    public partial class Manager : IReadOnlyDictionary<ActorIdentifier, ActiveDesign>
    {
        private readonly ActorManager _actors;

        private readonly Dictionary<ActorIdentifier, ActiveDesign> _characterSaves = new();

        public Manager(ActorManager actors)
            => _actors = actors;

        public IEnumerator<KeyValuePair<ActorIdentifier, ActiveDesign>> GetEnumerator()
            => _characterSaves.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public int Count
            => _characterSaves.Count;

        public bool ContainsKey(ActorIdentifier key)
            => _characterSaves.ContainsKey(key);

        public bool TryGetValue(ActorIdentifier key, [NotNullWhen(true)] out ActiveDesign? value)
            => _characterSaves.TryGetValue(key, out value);

        public ActiveDesign this[ActorIdentifier key]
            => _characterSaves[key];

        public IEnumerable<ActorIdentifier> Keys
            => _characterSaves.Keys;

        public IEnumerable<ActiveDesign> Values
            => _characterSaves.Values;

        public void DeleteSave(ActorIdentifier identifier)
            => _characterSaves.Remove(identifier);

        public ActiveDesign GetOrCreateSave(Actor actor)
        {
            var id = actor.GetIdentifier();
            if (_characterSaves.TryGetValue(id, out var save))
            {
                save.Update(actor);
                return save;
            }

            save = new ActiveDesign();
            save.Update(actor);
            _characterSaves.Add(id.CreatePermanent(), save);
            return save;
        }
    }
}
