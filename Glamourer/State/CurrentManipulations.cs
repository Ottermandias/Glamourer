using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Glamourer.Interop;
using Penumbra.GameData.Actors;

namespace Glamourer.State;

public class CurrentManipulations : IReadOnlyCollection<KeyValuePair<ActorIdentifier, CurrentDesign>>
{
    private readonly Dictionary<ActorIdentifier, CurrentDesign> _characterSaves = new();

    public IEnumerator<KeyValuePair<ActorIdentifier, CurrentDesign>> GetEnumerator()
        => _characterSaves.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _characterSaves.Count;

    public CurrentDesign GetOrCreateSave(Actor actor)
    {
        var id = actor.GetIdentifier();
        if (_characterSaves.TryGetValue(id, out var save))
        {
            save.Update(actor);
            return save;
        }

        save = new CurrentDesign(actor);
        _characterSaves.Add(id.CreatePermanent(), save);
        return save;
    }

    public void DeleteSave(ActorIdentifier identifier)
        => _characterSaves.Remove(identifier);

    public bool TryGetDesign(ActorIdentifier identifier, [NotNullWhen(true)] out CurrentDesign? save)
        => _characterSaves.TryGetValue(identifier, out save);

    //public CharacterArmor? ChangeEquip(Actor actor, EquipSlot slot, CharacterArmor data)
    //{
    //    var save = CreateSave(actor);
    //    (_, data) = _restrictedGear.ResolveRestricted(data, slot, save.Customize.Race, save.Customize.Gender);
    //    if (save.Equipment[slot] == data)
    //        return null;
    //
    //    save.Equipment[slot] = data;
    //    return data;
    //}
    //
    //public bool ChangeWeapon(Actor actor, CharacterWeapon main)
    //{
    //    var save = CreateSave(actor);
    //    if (save.MainHand == main)
    //        return false;
    //
    //    save.MainHand = main;
    //    return true;
    //}
    //
    //public bool ChangeWeapon(Actor actor, CharacterWeapon main, CharacterWeapon off)
    //{
    //    var save = CreateSave(actor);
    //    if (main == save.MainHand && off == save.OffHand)
    //        return false;
    //
    //    save.MainHand = main;
    //    save.OffHand  = off;
    //    return true;
    //}
    //
}
