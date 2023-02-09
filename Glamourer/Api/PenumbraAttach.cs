using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Glamourer.Interop;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Glamourer.Api;

public unsafe class PenumbraAttach : IDisposable
{
    public const int RequiredPenumbraBreakingVersion = 4;
    public const int RequiredPenumbraFeatureVersion  = 15;

    private readonly EventSubscriber<ChangedItemType, uint>              _tooltipSubscriber;
    private readonly EventSubscriber<MouseButton, ChangedItemType, uint> _clickSubscriber;
    private readonly EventSubscriber<nint, string, nint, nint, nint>     _creatingCharacterBase;
    private readonly EventSubscriber<nint, string, nint>                 _createdCharacterBase;
    private          ActionSubscriber<int, RedrawType>                   _redrawSubscriber;
    private          FuncSubscriber<nint, (nint, string)>                _drawObjectInfo;
    private          FuncSubscriber<int, int>                            _cutsceneParent;

    private readonly EventSubscriber _initializedEvent;
    private readonly EventSubscriber _disposedEvent;
    public           bool            Available { get; private set; }

    public PenumbraAttach()
    {
        _initializedEvent      = Ipc.Initialized.Subscriber(Dalamud.PluginInterface, Reattach);
        _disposedEvent         = Ipc.Disposed.Subscriber(Dalamud.PluginInterface, Unattach);
        _tooltipSubscriber     = Ipc.ChangedItemTooltip.Subscriber(Dalamud.PluginInterface);
        _clickSubscriber       = Ipc.ChangedItemClick.Subscriber(Dalamud.PluginInterface);
        _createdCharacterBase  = Ipc.CreatedCharacterBase.Subscriber(Dalamud.PluginInterface);
        _creatingCharacterBase = Ipc.CreatingCharacterBase.Subscriber(Dalamud.PluginInterface);
        Reattach();
    }

    public event Action<MouseButton, ChangedItemType, uint> Click
    {
        add => _clickSubscriber.Event += value;
        remove => _clickSubscriber.Event -= value;
    }

    public event Action<ChangedItemType, uint> Tooltip
    {
        add => _tooltipSubscriber.Event += value;
        remove => _tooltipSubscriber.Event -= value;
    }


    public event Action<nint, string, nint, nint, nint> CreatingCharacterBase
    {
        add => _creatingCharacterBase.Event += value;
        remove => _creatingCharacterBase.Event -= value;
    }

    public event Action<nint, string, nint> CreatedCharacterBase
    {
        add => _createdCharacterBase.Event += value;
        remove => _createdCharacterBase.Event -= value;
    }

    public Actor GameObjectFromDrawObject(IntPtr drawObject)
        => Available ? _drawObjectInfo.Invoke(drawObject).Item1 : Actor.Null;

    public int CutsceneParent(int idx)
        => Available ? _cutsceneParent.Invoke(idx) : -1;

    public void RedrawObject(Actor actor, RedrawType settings)
    {
        if (!actor || !Available)
            return;

        try
        {
            _redrawSubscriber.Invoke(actor.Index, settings);
        }
        catch (Exception e)
        {
            PluginLog.Debug($"Failure redrawing object:\n{e}");
        }
    }

    public void Reattach()
    {
        try
        {
            Unattach();

            var (breaking, feature) = Ipc.ApiVersions.Subscriber(Dalamud.PluginInterface).Invoke();
            if (breaking != RequiredPenumbraBreakingVersion || feature < RequiredPenumbraFeatureVersion)
                throw new Exception(
                    $"Invalid Version {breaking}.{feature:D4}, required major Version {RequiredPenumbraBreakingVersion} with feature greater or equal to {RequiredPenumbraFeatureVersion}.");

            _tooltipSubscriber.Enable();
            _clickSubscriber.Enable();
            _creatingCharacterBase.Enable();
            _createdCharacterBase.Enable();
            _drawObjectInfo   = Ipc.GetDrawObjectInfo.Subscriber(Dalamud.PluginInterface);
            _cutsceneParent   = Ipc.GetCutsceneParentIndex.Subscriber(Dalamud.PluginInterface);
            _redrawSubscriber = Ipc.RedrawObjectByIndex.Subscriber(Dalamud.PluginInterface);
            Available         = true;
            Glamourer.Log.Debug("Glamourer attached to Penumbra.");
        }
        catch (Exception e)
        {
            Glamourer.Log.Debug($"Could not attach to Penumbra:\n{e}");
        }
    }

    public void Unattach()
    {
        _tooltipSubscriber.Disable();
        _clickSubscriber.Disable();
        _creatingCharacterBase.Disable();
        _createdCharacterBase.Disable();
        if (Available)
        {
            Available = false;
            Glamourer.Log.Debug("Glamourer detached from Penumbra.");
        }
    }

    public void Dispose()
    {
        Unattach();
        _tooltipSubscriber.Dispose();
        _clickSubscriber.Dispose();
        _creatingCharacterBase.Dispose();
        _createdCharacterBase.Dispose();
        _initializedEvent.Dispose();
        _disposedEvent.Dispose();
    }

    //private static void PenumbraTooltip(ChangedItemType type, uint _)
    //{
    //    if (type == ChangedItemType.Item)
    //        ImGui.Text("Right click to apply to current Glamourer Set. [Glamourer]");
    //}
    //
    //private void PenumbraRightClick(MouseButton button, ChangedItemType type, uint id)
    //{
    //    if (button != MouseButton.Right || type != ChangedItemType.Item)
    //        return;
    //
    //    var item      = (Lumina.Excel.GeneratedSheets.Item)type.GetObject(Dalamud.GameData, id)!;
    //    var writeItem = new Item2(item, string.Empty);
    //
    //    UpdateItem(_objects.GPosePlayer, writeItem);
    //    UpdateItem(_objects.Player,      writeItem);
    //}

    //private static void UpdateItem(Actor actor, Item2 item2)
    //{
    //    if (!actor || !actor.DrawObject)
    //        return;
    //
    //    switch (item2.EquippableTo)
    //    {
    //        case EquipSlot.MainHand:
    //        {
    //            var off = item2.HasSubModel
    //                ? new CharacterWeapon(item2.SubModel.id, item2.SubModel.type, item2.SubModel.variant, actor.DrawObject.OffHand.Stain)
    //                : item2.IsBothHand
    //                    ? CharacterWeapon.Empty
    //                    : actor.OffHand;
    //            var main = new CharacterWeapon(item2.MainModel.id, item2.MainModel.type, item2.MainModel.variant,
    //                actor.DrawObject.MainHand.Stain);
    //            Glamourer.RedrawManager.LoadWeapon(actor, main, off);
    //            return;
    //        }
    //        case EquipSlot.OffHand:
    //        {
    //            var off = new CharacterWeapon(item2.MainModel.id, item2.MainModel.type, item2.MainModel.variant,
    //                actor.DrawObject.OffHand.Stain);
    //            var main = actor.MainHand;
    //            Glamourer.RedrawManager.LoadWeapon(actor, main, off);
    //            return;
    //        }
    //        default:
    //        {
    //            var current = actor.DrawObject.Equip[item2.EquippableTo];
    //            var armor   = new CharacterArmor(item2.MainModel.id, (byte)item2.MainModel.variant, current.Stain);
    //            Glamourer.RedrawManager.UpdateSlot(actor.DrawObject, item2.EquippableTo, armor);
    //            return;
    //        }
    //    }
    //}

    // Update objects without triggering PlayerWatcher Events,
    // then manually redraw using Penumbra.
    public void UpdateCharacters(Character character, Character? gPoseOriginalCharacter = null)
    {
        //RedrawObject(character, RedrawType.Redraw, true);
        //
        //// Special case for carrying over changes to the gPose player to the regular player, too.
        //if (gPoseOriginalCharacter == null)
        //    return;
        //
        //newEquip.Write(gPoseOriginalCharacter.Address);
        //RedrawObject(gPoseOriginalCharacter, RedrawType.AfterGPose, false);
    }
}
