using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Interop.Structs;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Glamourer.Interop;

// TODO: Use client structs sigs.
public unsafe class ScalingService : IDisposable
{
    public ScalingService()
    {
        SignatureHelper.Initialise(this);
        _setupMountHook.Enable();
        _setupOrnamentHook.Enable();
    }

    public void Dispose()
    {
        _setupMountHook.Dispose();
        _setupOrnamentHook.Dispose();
    }

    private delegate void SetupMount(Character.MountContainer* container, short mountId, uint unk1, uint unk2, uint unk3, byte unk4);
    private delegate void SetupOrnament(Ornament* ornament, uint* unk1, float* unk2);

    [Signature("E8 ?? ?? ?? ?? 48 8B 43 ?? 80 B8 ?? ?? ?? ?? ?? 74 ?? 0F B6 90", DetourName = nameof(SetupMountDetour))]
    private readonly Hook<SetupMount> _setupMountHook = null!;

    [Signature("48 89 5C 24 ?? 41 54 41 56 41 57 48 83 EC ?? 4D 8B F8", DetourName = nameof(SetupOrnamentDetour))]
    private readonly Hook<SetupOrnament> _setupOrnamentHook = null!;

    [Signature("E8 ?? ?? ?? ?? 48 85 C0 48 0F 45 F8")]
    private readonly delegate* unmanaged<Ornament*, Character*> _getParentGameObject = null!;

    private void SetupMountDetour(Character.MountContainer* container, short mountId, uint unk1, uint unk2, uint unk3, byte unk4)
    {
        var (race, clan, gender) = GetRelevantCustomize(&container->OwnerObject->Character);
        SetCustomize(&container->OwnerObject->Character, container->OwnerObject->Character.GameObject.DrawObject);
        _setupMountHook.Original(container, mountId, unk1, unk2, unk3, unk4);
        SetCustomize(&container->OwnerObject->Character, race, clan, gender);
    }

    private void SetupOrnamentDetour(Ornament* ornament, uint* unk1, float* unk2)
    {
        var character = _getParentGameObject(ornament);
        if (character == null)
        {
            _setupOrnamentHook.Original(ornament, unk1, unk2);
            return;
        }

        var (race, clan, gender) = GetRelevantCustomize(character);
        SetCustomize(character, character->GameObject.DrawObject);
        _setupOrnamentHook.Original(ornament, unk1, unk2);
        SetCustomize(character, race, clan, gender);
    }

    /// <summary> We do not change the Customize gender because the functions use the GetGender() vfunc, which uses the game objects gender value. </summary>
    private static (byte Race, byte Clan, byte Gender) GetRelevantCustomize(Character* character)
        => (character->DrawData.CustomizeData.Race, character->DrawData.CustomizeData.Clan, character->GameObject.Gender);

    private static void SetCustomize(Character* character, Model model)
    {
        if (!model.IsHuman)
            return;

        SetCustomize(character, model.AsHuman->Customize.Race, model.AsHuman->Customize.Clan, model.AsHuman->Customize.Sex);
    }

    private static void SetCustomize(Character* character, byte race, byte clan, byte gender)
    {
        character->DrawData.CustomizeData.Race = race;
        character->DrawData.CustomizeData.Clan = clan;
        character->GameObject.Gender           = gender;
    }
}
