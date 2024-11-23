using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Penumbra.GameData;
using Penumbra.GameData.Interop;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Glamourer.Interop;

public unsafe class ScalingService : IDisposable
{
    public ScalingService(IGameInteropProvider interop)
    {
        interop.InitializeFromAttributes(this);
        _setupMountHook =
            interop.HookFromAddress<SetupMount>((nint)MountContainer.MemberFunctionPointers.SetupMount, SetupMountDetour);
        _calculateHeightHook =
            interop.HookFromAddress<CalculateHeight>((nint)ModelContainer.MemberFunctionPointers.CalculateHeight, CalculateHeightDetour);

        _setupMountHook.Enable();
        _updateOrnamentHook.Enable();
        _placeMinionHook.Enable();
        _calculateHeightHook.Enable();
    }

    public void Dispose()
    {
        _setupMountHook.Dispose();
        _updateOrnamentHook.Dispose();
        _placeMinionHook.Dispose();
        _calculateHeightHook.Dispose();
    }

    private delegate void  SetupMount(MountContainer* container, short mountId, uint unk1, uint unk2, uint unk3, byte unk4);
    private delegate void  UpdateOrnament(OrnamentContainer* ornament);
    private delegate void  PlaceMinion(Companion* character);
    private delegate float CalculateHeight(ModelContainer* character);

    private readonly Hook<SetupMount> _setupMountHook;

    // TODO: Use client structs sig.
    [Signature(Sigs.UpdateOrnament, DetourName = nameof(UpdateOrnamentDetour))]
    private readonly Hook<UpdateOrnament> _updateOrnamentHook = null!;

    private readonly Hook<CalculateHeight> _calculateHeightHook;

    // TODO: Use client structs sig.
    [Signature(Sigs.PlaceMinion, DetourName = nameof(PlaceMinionDetour))]
    private readonly Hook<PlaceMinion> _placeMinionHook = null!;

    private void SetupMountDetour(MountContainer* container, short mountId, uint unk1, uint unk2, uint unk3, byte unk4)
    {
        var (race, clan, gender) = GetScaleRelevantCustomize(container->OwnerObject);
        SetScaleCustomize(container->OwnerObject, container->OwnerObject->DrawObject);
        _setupMountHook.Original(container, mountId, unk1, unk2, unk3, unk4);
        SetScaleCustomize(container->OwnerObject, race, clan, gender);
    }

    private void UpdateOrnamentDetour(OrnamentContainer* container)
    {
        var (race, clan, gender) = GetScaleRelevantCustomize(container->OwnerObject);
        SetScaleCustomize(container->OwnerObject, container->OwnerObject->DrawObject);
        _updateOrnamentHook.Original(container);
        SetScaleCustomize(container->OwnerObject, race, clan, gender);
    }

    private void PlaceMinionDetour(Companion* companion)
    {
        var owner = (Actor)(GameObject*)companion->Owner;
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

    private float CalculateHeightDetour(ModelContainer* container)
    {
        var (gender, bodyType, clan, height) = GetHeightRelevantCustomize(container->OwnerObject);
        SetHeightCustomize(container->OwnerObject, container->OwnerObject->DrawObject);
        var ret = _calculateHeightHook.Original(container);
        SetHeightCustomize(container->OwnerObject, gender, bodyType, clan, height);
        return ret;
    }

    /// <summary> We do not change the Customize gender because the functions use the GetGender() vfunc, which uses the game objects gender value. </summary>
    private static (byte Race, byte Clan, byte Gender) GetScaleRelevantCustomize(Character* character)
        => (character->DrawData.CustomizeData.Race, character->DrawData.CustomizeData.Tribe, character->GameObject.Sex);

    private static (byte Gender, byte BodyType, byte Clan, byte Height) GetHeightRelevantCustomize(Character* character)
        => (character->DrawData.CustomizeData.Sex, character->DrawData.CustomizeData.BodyType,
            character->DrawData.CustomizeData.Tribe, character->DrawData.CustomizeData[(int)CustomizeIndex.Height]);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetScaleCustomize(Character* character, Model model)
    {
        if (!model.IsHuman)
            return;

        SetScaleCustomize(character, model.AsHuman->Customize.Race, model.AsHuman->Customize.Tribe, model.AsHuman->Customize.Sex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetScaleCustomize(Character* character, byte race, byte clan, byte gender)
    {
        character->DrawData.CustomizeData.Race  = race;
        character->DrawData.CustomizeData.Tribe = clan;
        character->GameObject.Sex               = gender;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetHeightCustomize(Character* character, Model model)
    {
        if (!model.IsHuman)
            return;

        SetHeightCustomize(character, model.AsHuman->Customize.Sex, model.AsHuman->Customize.BodyType, model.AsHuman->Customize.Tribe,
            model.AsHuman->Customize[(int)CustomizeIndex.Height]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetHeightCustomize(Character* character, byte gender, byte bodyType, byte clan, byte height)
    {
        character->DrawData.CustomizeData.Sex      = gender;
        character->DrawData.CustomizeData.BodyType = bodyType;
        character->DrawData.CustomizeData.Tribe    = clan;
        character->DrawData.CustomizeData.Height   = height;
    }
}
