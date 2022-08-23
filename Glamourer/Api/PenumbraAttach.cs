using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin.Ipc;
using Glamourer.Interop;
using Glamourer.Structs;
using ImGuiNET;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Api;

public class PenumbraAttach : IDisposable
{
    public const int RequiredPenumbraBreakingVersion = 4;
    public const int RequiredPenumbraFeatureVersion  = 12;

    private ICallGateSubscriber<ChangedItemType, uint, object>?                   _tooltipSubscriber;
    private ICallGateSubscriber<MouseButton, ChangedItemType, uint, object>?      _clickSubscriber;
    private ICallGateSubscriber<string, int, object>?                             _redrawSubscriberName;
    private ICallGateSubscriber<GameObject, int, object>?                         _redrawSubscriberObject;
    private ICallGateSubscriber<IntPtr, (IntPtr, string)>?                        _drawObjectInfo;
    private ICallGateSubscriber<IntPtr, string, IntPtr, IntPtr, IntPtr, object?>? _creatingCharacterBase;
    private ICallGateSubscriber<IntPtr, string, IntPtr, object?>?                 _createdCharacterBase;
    private ICallGateSubscriber<int, int>?                                        _cutsceneParent;

    private readonly ICallGateSubscriber<object?> _initializedEvent;
    private readonly ICallGateSubscriber<object?> _disposedEvent;

    public event Action<IntPtr, IntPtr, IntPtr, IntPtr>? CreatingCharacterBase;
    public event Action<IntPtr, IntPtr>?                 CreatedCharacterBase;

    public PenumbraAttach(bool attach)
    {
        _initializedEvent = Dalamud.PluginInterface.GetIpcSubscriber<object?>("Penumbra.Initialized");
        _disposedEvent    = Dalamud.PluginInterface.GetIpcSubscriber<object?>("Penumbra.Disposed");
        _initializedEvent.Subscribe(Reattach);
        _disposedEvent.Subscribe(Unattach);
        Reattach(attach);
    }

    private void Reattach()
        => Reattach(Glamourer.Config.AttachToPenumbra);

    public void Reattach(bool attach)
    {
        try
        {
            Unattach();

            var versionSubscriber = Dalamud.PluginInterface.GetIpcSubscriber<(int, int)>("Penumbra.ApiVersions");
            var (breaking, feature) = versionSubscriber.InvokeFunc();
            if (breaking != RequiredPenumbraBreakingVersion || feature < RequiredPenumbraFeatureVersion)
                throw new Exception(
                    $"Invalid Version {breaking}.{feature:D4}, required major Version {RequiredPenumbraBreakingVersion} with feature greater or equal to {RequiredPenumbraFeatureVersion}.");

            _redrawSubscriberName   = Dalamud.PluginInterface.GetIpcSubscriber<string, int, object>("Penumbra.RedrawObjectByName");
            _redrawSubscriberObject = Dalamud.PluginInterface.GetIpcSubscriber<GameObject, int, object>("Penumbra.RedrawObject");
            _drawObjectInfo         = Dalamud.PluginInterface.GetIpcSubscriber<IntPtr, (IntPtr, string)>("Penumbra.GetDrawObjectInfo");
            _cutsceneParent         = Dalamud.PluginInterface.GetIpcSubscriber<int, int>("Penumbra.GetCutsceneParentIndex");

            if (!attach)
                return;

            _tooltipSubscriber = Dalamud.PluginInterface.GetIpcSubscriber<ChangedItemType, uint, object>("Penumbra.ChangedItemTooltip");
            _clickSubscriber =
                Dalamud.PluginInterface.GetIpcSubscriber<MouseButton, ChangedItemType, uint, object>("Penumbra.ChangedItemClick");
            _creatingCharacterBase =
                Dalamud.PluginInterface.GetIpcSubscriber<IntPtr, string, IntPtr, IntPtr, IntPtr, object?>("Penumbra.CreatingCharacterBase");
            _createdCharacterBase =
                Dalamud.PluginInterface.GetIpcSubscriber<IntPtr, string, IntPtr, object?>("Penumbra.CreatedCharacterBase");
            _tooltipSubscriber.Subscribe(PenumbraTooltip);
            _clickSubscriber.Subscribe(PenumbraRightClick);
            _creatingCharacterBase.Subscribe(SubscribeCreatingCharacterBase);
            _createdCharacterBase.Subscribe(SubscribeCreatedCharacterBase);
            PluginLog.Debug("Glamourer attached to Penumbra.");
        }
        catch (Exception e)
        {
            PluginLog.Debug($"Could not attach to Penumbra:\n{e}");
        }
    }

    private void SubscribeCreatingCharacterBase(IntPtr gameObject, string _, IntPtr modelId, IntPtr customize, IntPtr equipment)
        => CreatingCharacterBase?.Invoke(gameObject, modelId, customize, equipment);

    private void SubscribeCreatedCharacterBase(IntPtr gameObject, string _, IntPtr drawObject)
        => CreatedCharacterBase?.Invoke(gameObject, drawObject);

    public void Unattach()
    {
        _tooltipSubscriber?.Unsubscribe(PenumbraTooltip);
        _clickSubscriber?.Unsubscribe(PenumbraRightClick);
        _creatingCharacterBase?.Unsubscribe(SubscribeCreatingCharacterBase);
        _createdCharacterBase?.Unsubscribe(SubscribeCreatedCharacterBase);
        _tooltipSubscriber     = null;
        _clickSubscriber       = null;
        _creatingCharacterBase = null;
        _redrawSubscriberName  = null;
        _drawObjectInfo        = null;
        if (_redrawSubscriberObject != null)
        {
            PluginLog.Debug("Glamourer detached from Penumbra.");
            _redrawSubscriberObject = null;
        }
    }

    public void Dispose()
    {
        _initializedEvent.Unsubscribe(Reattach);
        _disposedEvent.Unsubscribe(Unattach);
        Unattach();
    }

    private static void PenumbraTooltip(ChangedItemType type, uint _)
    {
        if (type == ChangedItemType.Item)
            ImGui.Text("Right click to apply to current Glamourer Set. [Glamourer]");
    }

    private void PenumbraRightClick(MouseButton button, ChangedItemType type, uint id)
    {
        if (button != MouseButton.Right || type != ChangedItemType.Item)
            return;

        var item      = (Lumina.Excel.GeneratedSheets.Item)type.GetObject(id)!;
        var writeItem = new Item(item, string.Empty);

        UpdateItem(ObjectManager.GPosePlayer, writeItem);
        UpdateItem(ObjectManager.Player,      writeItem);
    }

    private static void UpdateItem(Actor actor, Item item)
    {
        if (!actor || !actor.DrawObject)
            return;

        switch (item.EquippableTo)
        {
            case EquipSlot.MainHand:
            {
                var off = item.HasSubModel
                    ? new CharacterWeapon(item.SubModel.id, item.SubModel.type, item.SubModel.variant, actor.DrawObject.OffHand.Stain)
                    : item.IsBothHand
                        ? CharacterWeapon.Empty
                        : actor.OffHand;
                var main = new CharacterWeapon(item.MainModel.id, item.MainModel.type, item.MainModel.variant, actor.DrawObject.MainHand.Stain);
                Glamourer.RedrawManager.LoadWeapon(actor, main, off);
                return;
            }
            case EquipSlot.OffHand:
            {
                var off  = new CharacterWeapon(item.MainModel.id, item.MainModel.type, item.MainModel.variant, actor.DrawObject.OffHand.Stain);
                var main = actor.MainHand;
                Glamourer.RedrawManager.LoadWeapon(actor, main, off);
                return;
            }
            default:
            {
                var current = actor.DrawObject.Equip[item.EquippableTo];
                var armor   = new CharacterArmor(item.MainModel.id, (byte)item.MainModel.variant, current.Stain);
                Glamourer.RedrawManager.ChangeEquip(actor.DrawObject, item.EquippableTo, armor);
                return;
            }
        }
    }

    public Actor GameObjectFromDrawObject(IntPtr drawObject)
        => _drawObjectInfo?.InvokeFunc(drawObject).Item1 ?? IntPtr.Zero;

    public int CutsceneParent(int idx)
        => _cutsceneParent?.InvokeFunc(idx) ?? -1;

    public void RedrawObject(GameObject? actor, RedrawType settings, bool repeat)
    {
        if (actor == null)
            return;

        if (_redrawSubscriberObject != null)
        {
            try
            {
                _redrawSubscriberObject.InvokeAction(actor, (int)settings);
            }
            catch (Exception e)
            {
                if (repeat)
                {
                    Reattach(Glamourer.Config.AttachToPenumbra);
                    RedrawObject(actor, settings, false);
                }
                else
                {
                    PluginLog.Debug($"Failure redrawing object:\n{e}");
                }
            }
        }
        else if (repeat)
        {
            Reattach(Glamourer.Config.AttachToPenumbra);
            RedrawObject(actor, settings, false);
        }
        else
        {
            PluginLog.Debug("Trying to redraw object, but not attached to Penumbra.");
        }
    }

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
