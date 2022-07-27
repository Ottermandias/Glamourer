using System.Collections.Concurrent;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;

namespace Glamourer.Designs
{
    public class RevertableDesigns
    {
        public readonly ConcurrentDictionary<string, CharacterSave> Saves = new();

        public bool Add(Character actor)
        {
            var name = actor.Name.ToString();
            if (Saves.TryGetValue(name, out var save))
                return false;

            save = new CharacterSave();
            save.LoadCharacter(actor);
            Saves[name] = save;
            return true;
        }

        public bool RevertByNameWithoutApplication(string actorName)
        {
            if (!Saves.ContainsKey(actorName))
                return false;

            Saves.Remove(actorName, out _);
            return true;
        }

        public bool Revert(Character actor)
        {
            if (!Saves.TryGetValue(actor.Name.ToString(), out var save))
                return false;

            save.Apply(actor);
            Saves.Remove(actor.Name.ToString(), out _);
            return true;
        }
    }
}
