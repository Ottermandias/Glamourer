using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Glamourer.Interop;

// TODO: Use clientstructs sigs.
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
    private readonly delegate* unmanaged<Ornament*, Character*> GetParentGameObject = null!;

    private void SetupMountDetour(Character.MountContainer* container, short mountId, uint unk1, uint unk2, uint unk3, byte unk4)
    {
        var oldRace    = container->OwnerObject->Character.DrawData.CustomizeData.Race;
        var oldSex     = container->OwnerObject->Character.DrawData.CustomizeData.Sex;
        var oldClan    = container->OwnerObject->Character.DrawData.CustomizeData.Clan;
        var drawObject = container->OwnerObject->Character.GameObject.DrawObject;
        if (drawObject != null
         && drawObject->Object.GetObjectType() is ObjectType.CharacterBase
         && ((CharacterBase*)drawObject)->GetModelType() is CharacterBase.ModelType.Human)
        {
            container->OwnerObject->Character.DrawData.CustomizeData.Race = ((Human*)drawObject)->Customize.Race;
            container->OwnerObject->Character.DrawData.CustomizeData.Sex  = ((Human*)drawObject)->Customize.Sex;
            container->OwnerObject->Character.DrawData.CustomizeData.Clan = ((Human*)drawObject)->Customize.Clan;
        }

        _setupMountHook.Original(container, mountId, unk1, unk2, unk3, unk4);
        container->OwnerObject->Character.DrawData.CustomizeData.Race = oldRace;
        container->OwnerObject->Character.DrawData.CustomizeData.Sex  = oldSex;
        container->OwnerObject->Character.DrawData.CustomizeData.Clan = oldClan;
    }

    private void SetupOrnamentDetour(Ornament* ornament, uint* unk1, float* unk2)
    {
        var character = GetParentGameObject(ornament);
        if (character == null)
        {
            _setupOrnamentHook.Original(ornament, unk1, unk2);
            return;
        }

        var oldRace    = character->DrawData.CustomizeData.Race;
        var oldSex     = character->DrawData.CustomizeData.Sex;
        var oldClan    = character->DrawData.CustomizeData.Clan;
        var drawObject = character->GameObject.DrawObject;
        if (drawObject != null
         && drawObject->Object.GetObjectType() is ObjectType.CharacterBase
         && ((CharacterBase*)drawObject)->GetModelType() is CharacterBase.ModelType.Human)
        {
            character->DrawData.CustomizeData.Race = ((Human*)drawObject)->Customize.Race;
            character->DrawData.CustomizeData.Sex  = ((Human*)drawObject)->Customize.Sex;
            character->DrawData.CustomizeData.Clan = ((Human*)drawObject)->Customize.Clan;
        }

        _setupOrnamentHook.Original(ornament, unk1, unk2);
        character->DrawData.CustomizeData.Race = oldRace;
        character->DrawData.CustomizeData.Sex  = oldSex;
        character->DrawData.CustomizeData.Clan = oldClan;
    }
}
