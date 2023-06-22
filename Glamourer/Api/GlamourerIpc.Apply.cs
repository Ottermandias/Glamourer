using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Designs;
using Glamourer.Interop.Structs;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelApplyAll                          = "Glamourer.ApplyAll";
    public const string LabelApplyAllToCharacter               = "Glamourer.ApplyAllToCharacter";
    public const string LabelApplyOnlyEquipment                = "Glamourer.ApplyOnlyEquipment";
    public const string LabelApplyOnlyEquipmentToCharacter     = "Glamourer.ApplyOnlyEquipmentToCharacter";
    public const string LabelApplyOnlyCustomization            = "Glamourer.ApplyOnlyCustomization";
    public const string LabelApplyOnlyCustomizationToCharacter = "Glamourer.ApplyOnlyCustomizationToCharacter";

    private readonly ActionProvider<string, string>     _applyAllProvider;
    private readonly ActionProvider<string, Character?> _applyAllToCharacterProvider;
    private readonly ActionProvider<string, string>     _applyOnlyEquipmentProvider;
    private readonly ActionProvider<string, Character?> _applyOnlyEquipmentToCharacterProvider;
    private readonly ActionProvider<string, string>     _applyOnlyCustomizationProvider;
    private readonly ActionProvider<string, Character?> _applyOnlyCustomizationToCharacterProvider;

    public static ActionSubscriber<string, string> ApplyAllSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyAll);

    public static ActionSubscriber<string, Character?> ApplyAllToCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyAllToCharacter);

    public static ActionSubscriber<string, string> ApplyOnlyEquipmentSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyOnlyEquipment);

    public static ActionSubscriber<string, Character?> ApplyOnlyEquipmentToCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyOnlyEquipmentToCharacter);

    public static ActionSubscriber<string, string> ApplyOnlyCustomizationSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyOnlyCustomization);

    public static ActionSubscriber<string, Character?> ApplyOnlyCustomizationToCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApplyOnlyCustomizationToCharacter);


    public void ApplyAll(string base64, string characterName)
        => ApplyDesign(CreateTemporaryFromBase64(base64, true, true), FindActors(characterName));

    public void ApplyAllToCharacter(string base64, Character? character)
        => ApplyDesign(CreateTemporaryFromBase64(base64, true, true), FindActors(character));

    public void ApplyOnlyEquipment(string base64, string characterName)
        => ApplyDesign(CreateTemporaryFromBase64(base64, false, true), FindActors(characterName));

    public void ApplyOnlyEquipmentToCharacter(string base64, Character? character)
        => ApplyDesign(CreateTemporaryFromBase64(base64, false, true), FindActors(character));

    public void ApplyOnlyCustomization(string base64, string characterName)
        => ApplyDesign(CreateTemporaryFromBase64(base64, true, false), FindActors(characterName));

    public void ApplyOnlyCustomizationToCharacter(string base64, Character? character)
        => ApplyDesign(CreateTemporaryFromBase64(base64, true, false), FindActors(character));

    private void ApplyDesign(Design? design, IEnumerable<ActorIdentifier> actors)
    {
        if (design == null)
            return;

        _objects.Update();
        foreach (var id in actors)
        {
            if (!_stateManager.TryGetValue(id, out var state))
            {
                var data = _objects.TryGetValue(id, out var d) ? d : ActorData.Invalid;
                if (!data.Valid || !_stateManager.GetOrCreate(id, data.Objects[0], out state))
                    continue;
            }

            _stateManager.ApplyDesign(design, state);
        }
    }

    private Design? CreateTemporaryFromBase64(string base64, bool customize, bool equip)
    {
        try
        {
            var ret = new Design(_items);
            ret.MigrateBase64(_items, base64);
            if (!customize)
            {
                ret.ApplyCustomize = 0;
                ret.SetApplyWetness(false);
            }

            if (!equip)
            {
                ret.ApplyEquip = 0;
                ret.SetApplyHatVisible(false);
                ret.SetApplyWeaponVisible(false);
                ret.SetApplyVisorToggle(false);
            }

            return ret;
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"[IPC] Could not parse base64 string [{base64}]:\n{ex}");
            return null;
        }
    }
}
