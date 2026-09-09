using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Penumbra.GameData;
using Penumbra.GameData.Interop;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.State;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using CustomizeIndex = Dalamud.Game.ClientState.Objects.Enums.CustomizeIndex;

namespace Glamourer.Interop;

public sealed unsafe class ScalingService : IDisposable, IRequiredService
{
    private readonly HookManager  _hooks;
    private readonly ActorManager _actors;
    private readonly StateManager _state;

    public ScalingService(StateManager state, ActorManager actors, HookManager hooks)
    {
        _state  = state;
        _actors = actors;
        _hooks  = hooks;
        _setupMountHook = _hooks.CreateHook<SetupMount>("SetupMount", (nint)MountContainer.MemberFunctionPointers.SetupMount, SetupMountDetour, true)!;
        _calculateHeightHook = _hooks.CreateHook<CalculateHeight>("CalculateHeight", (nint)ModelContainer.MemberFunctionPointers.CalculateHeight, CalculateHeightDetour, true)!;
        _placeMinionHook = _hooks.CreateHook<PlaceMinion>("PlaceMinion", (nint)Companion.MemberFunctionPointers.PlaceCompanion, PlaceMinionDetour, true)!;
        _updateOrnamentHook = _hooks.CreateHook<UpdateOrnament>("UpdTateOrnament", Sigs.UpdateOrnament, UpdateOrnamentDetour, true)!;
    }

    public void Dispose()
    {
        _hooks.DisposeHook("SetupMount");
        _hooks.DisposeHook("CalculateHeight");
        _hooks.DisposeHook("PlaceMinion");
        _hooks.DisposeHook("UpdateOrnament");
    }

    private delegate void  SetupMount(MountContainer* container, short mountId, uint unk1, uint unk2, uint unk3, byte unk4);
    private delegate void  UpdateOrnament(OrnamentContainer* ornament);
    private delegate void  PlaceMinion(Companion* character);
    private delegate float CalculateHeight(ModelContainer* character);

    private readonly Task<Hook<SetupMount>> _setupMountHook;
    private readonly Task<Hook<UpdateOrnament>> _updateOrnamentHook;
    private readonly Task<Hook<CalculateHeight>> _calculateHeightHook;
    private readonly Task<Hook<PlaceMinion>> _placeMinionHook;

    private void SetupMountDetour(MountContainer* container, short mountId, uint unk1, uint unk2, uint unk3, byte unk4)
    {
        var (race, clan, gender) = GetScaleRelevantCustomize(container->OwnerObject);
        SetScaleCustomize(container->OwnerObject, container->OwnerObject->DrawObject);
        _setupMountHook.Result.Original(container, mountId, unk1, unk2, unk3, unk4);
        SetScaleCustomize(container->OwnerObject, race, clan, gender);
    }

    private void UpdateOrnamentDetour(OrnamentContainer* container)
    {
        var (race, clan, gender) = GetScaleRelevantCustomize(container->OwnerObject);
        SetScaleCustomize(container->OwnerObject, container->OwnerObject->DrawObject);
        _updateOrnamentHook.Result.Original(container);
        SetScaleCustomize(container->OwnerObject, race, clan, gender);
    }

    private void PlaceMinionDetour(Companion* companion)
    {
        var owner = (Actor)(GameObject*)companion->Owner;
        if (!owner.IsCharacter)
        {
            _placeMinionHook.Result.Original(companion);
        }
        else
        {
            var mdl     = owner.Model;
            var oldRace = owner.AsCharacter->DrawData.CustomizeData.Race;
            if (mdl.IsHuman)
            {
                owner.AsCharacter->DrawData.CustomizeData.Race = mdl.AsHuman->Customize.Race;
            }
            else
            {
                var actor = _actors.FromObject(owner, out _, true, false, true);
                if (_state.TryGetValue(actor, out var state))
                    owner.AsCharacter->DrawData.CustomizeData.Race = (byte)state.ModelData.Customize.Race;
            }

            _placeMinionHook.Result.Original(companion);
            owner.AsCharacter->DrawData.CustomizeData.Race = oldRace;
        }
    }

    private float CalculateHeightDetour(ModelContainer* container)
    {
        var (gender, bodyType, clan, height) = GetHeightRelevantCustomize(container->OwnerObject);
        SetHeightCustomize(container->OwnerObject, container->OwnerObject->DrawObject);
        var ret = _calculateHeightHook.Result.Original(container);
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
    private void SetScaleCustomize(Character* character, Model model)
    {
        if (model.IsHuman)
        {
            SetScaleCustomize(character, model.AsHuman->Customize.Race, model.AsHuman->Customize.Tribe, model.AsHuman->Customize.Sex);
            return;
        }

        var actor = _actors.FromObject(character, out _, true, false, true);
        if (!_state.TryGetValue(actor, out var state))
            return;

        ref var customize = ref state.ModelData.Customize;
        SetScaleCustomize(character, (byte)customize.Race, (byte)customize.Clan, customize.Gender.ToGameByte());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetScaleCustomize(Character* character, byte race, byte clan, byte gender)
    {
        character->DrawData.CustomizeData.Race  = race;
        character->DrawData.CustomizeData.Tribe = clan;
        character->GameObject.Sex               = gender;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void SetHeightCustomize(Character* character, Model model)
    {
        if (model.IsHuman)
        {
            SetHeightCustomize(character, model.AsHuman->Customize.Sex, model.AsHuman->Customize.BodyType, model.AsHuman->Customize.Tribe,
                model.AsHuman->Customize[(int)CustomizeIndex.Height]);
            return;
        }

        var actor = _actors.FromObject(character, out _, true, false, true);
        if (!_state.TryGetValue(actor, out var state))
            return;

        ref var customize = ref state.ModelData.Customize;
        SetHeightCustomize(character, customize.Gender.ToGameByte(), customize.BodyType.Value, (byte)customize.Clan,
            customize[global::Penumbra.GameData.Enums.CustomizeIndex.Height].Value);
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
