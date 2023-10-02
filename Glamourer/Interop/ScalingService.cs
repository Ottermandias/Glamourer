using System;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Interop.Structs;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Glamourer.Interop;

public unsafe class ScalingService : IDisposable
{
    public ScalingService(IGameInteropProvider interop)
    {
        interop.InitializeFromAttributes(this);
        _setupMountHook =
            interop.HookFromAddress<SetupMount>((nint)Character.MountContainer.MemberFunctionPointers.SetupMount, SetupMountDetour);
        _setupOrnamentHook = interop.HookFromAddress<SetupOrnament>((nint)Ornament.MemberFunctionPointers.SetupOrnament, SetupOrnamentDetour);
        _calculateHeightHook =
            interop.HookFromAddress<CalculateHeight>((nint)Character.MemberFunctionPointers.CalculateHeight, CalculateHeightDetour);
        _setupMountHook.Enable();
        _setupOrnamentHook.Enable();
        _placeMinionHook.Enable();
        _calculateHeightHook.Enable();
    }

    public void Dispose()
    {
        _setupMountHook.Dispose();
        _setupOrnamentHook.Dispose();
        _placeMinionHook.Dispose();
        _calculateHeightHook.Dispose();
    }

    private delegate void  SetupMount(Character.MountContainer* container, short mountId, uint unk1, uint unk2, uint unk3, byte unk4);
    private delegate void  SetupOrnament(Ornament* ornament, uint* unk1, float* unk2);
    private delegate void  PlaceMinion(Companion* character);
    private delegate float CalculateHeight(Character* character);

    private readonly Hook<SetupMount> _setupMountHook;

    private readonly Hook<SetupOrnament> _setupOrnamentHook;

    private readonly Hook<CalculateHeight> _calculateHeightHook;

    // TODO: Use client structs sig.
    [Signature("48 89 5C 24 ?? 55 57 41 57 48 8D 6C 24", DetourName = nameof(PlaceMinionDetour))]
    private readonly Hook<PlaceMinion> _placeMinionHook = null!;


    private void SetupMountDetour(Character.MountContainer* container, short mountId, uint unk1, uint unk2, uint unk3, byte unk4)
    {
        var (race, clan, gender) = GetScaleRelevantCustomize(&container->OwnerObject->Character);
        SetScaleCustomize(&container->OwnerObject->Character, container->OwnerObject->Character.GameObject.DrawObject);
        _setupMountHook.Original(container, mountId, unk1, unk2, unk3, unk4);
        SetScaleCustomize(&container->OwnerObject->Character, race, clan, gender);
    }

    private void SetupOrnamentDetour(Ornament* ornament, uint* unk1, float* unk2)
    {
        var character = ornament->Character.GetParentCharacter();
        if (character == null)
        {
            _setupOrnamentHook.Original(ornament, unk1, unk2);
            return;
        }

        var (race, clan, gender) = GetScaleRelevantCustomize(character);
        SetScaleCustomize(character, character->GameObject.DrawObject);
        _setupOrnamentHook.Original(ornament, unk1, unk2);
        SetScaleCustomize(character, race, clan, gender);
    }

    private void PlaceMinionDetour(Companion* companion)
    {
        var owner = (Actor)((nint*)companion)[0x374];
        if (!owner.IsCharacter)
        {
            _placeMinionHook.Original(companion);
        }
        else
        {
            var mdl     = owner.Model;
            var oldRace = owner.AsCharacter->DrawData.CustomizeData.Race;
            if (mdl.IsHuman)
                owner.AsCharacter->DrawData.CustomizeData.Race = mdl.AsHuman->Customize.Race;
            _placeMinionHook.Original(companion);
            owner.AsCharacter->DrawData.CustomizeData.Race = oldRace;
        }
    }

    private float CalculateHeightDetour(Character* character)
    {
        var (gender, bodyType, clan, height) = GetHeightRelevantCustomize(character);
        SetHeightCustomize(character, character->GameObject.DrawObject);
        var ret = _calculateHeightHook.Original(character);
        SetHeightCustomize(character, gender, bodyType, clan, height);
        return ret;
    }

    /// <summary> We do not change the Customize gender because the functions use the GetGender() vfunc, which uses the game objects gender value. </summary>
    private static (byte Race, byte Clan, byte Gender) GetScaleRelevantCustomize(Character* character)
        => (character->DrawData.CustomizeData.Race, character->DrawData.CustomizeData.Clan, character->GameObject.Gender);

    private static (byte Gender, byte BodyType, byte Clan, byte Height) GetHeightRelevantCustomize(Character* character)
        => (character->DrawData.CustomizeData.Sex, character->DrawData.CustomizeData.BodyType,
            character->DrawData.CustomizeData.Clan, character->DrawData.CustomizeData[(int)CustomizeIndex.Height]);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetScaleCustomize(Character* character, Model model)
    {
        if (!model.IsHuman)
            return;

        SetScaleCustomize(character, model.AsHuman->Customize.Race, model.AsHuman->Customize.Clan, model.AsHuman->Customize.Sex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetScaleCustomize(Character* character, byte race, byte clan, byte gender)
    {
        character->DrawData.CustomizeData.Race = race;
        character->DrawData.CustomizeData.Clan = clan;
        character->GameObject.Gender           = gender;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetHeightCustomize(Character* character, Model model)
    {
        if (!model.IsHuman)
            return;

        SetHeightCustomize(character, model.AsHuman->Customize.Sex, model.AsHuman->Customize.BodyType, model.AsHuman->Customize.Clan,
            model.AsHuman->Customize[(int)CustomizeIndex.Height]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetHeightCustomize(Character* character, byte gender, byte bodyType, byte clan, byte height)
    {
        character->DrawData.CustomizeData.Sex                              = gender;
        character->DrawData.CustomizeData.BodyType                         = bodyType;
        character->DrawData.CustomizeData.Clan                             = clan;
        character->DrawData.CustomizeData.Data[(int)CustomizeIndex.Height] = height;
    }
}
