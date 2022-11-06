using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Glamourer.Interop;
using Glamourer.Structs;
using ImGuiNET;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Api;

public unsafe class PenumbraAttach : IDisposable
{
    public const int RequiredPenumbraBreakingVersion = 4;
    public const int RequiredPenumbraFeatureVersion  = 15;

    private EventSubscriber<ChangedItemType, uint>              _tooltipSubscriber;
    private EventSubscriber<MouseButton, ChangedItemType, uint> _clickSubscriber;
    private ActionSubscriber<GameObject, RedrawType>            _redrawSubscriber;
    private FuncSubscriber<nint, (nint, string)>                _drawObjectInfo;
    public  EventSubscriber<nint, string, nint, nint, nint>     CreatingCharacterBase;
    public  EventSubscriber<nint, string, nint>                 CreatedCharacterBase;
    private FuncSubscriber<int, int>                            _cutsceneParent;

    private readonly EventSubscriber _initializedEvent;
    private readonly EventSubscriber _disposedEvent;
    public           bool            Available { get; private set; }

    public PenumbraAttach(bool attach)
    {
        _initializedEvent    = Ipc.Initialized.Subscriber(Dalamud.PluginInterface, Reattach);
        _disposedEvent       = Ipc.Disposed.Subscriber(Dalamud.PluginInterface, Unattach);
        _tooltipSubscriber   = Ipc.ChangedItemTooltip.Subscriber(Dalamud.PluginInterface, PenumbraTooltip);
        _clickSubscriber     = Ipc.ChangedItemClick.Subscriber(Dalamud.PluginInterface, PenumbraRightClick);
        CreatedCharacterBase = Ipc.CreatedCharacterBase.Subscriber(Dalamud.PluginInterface);
        CreatingCharacterBase = Ipc.CreatingCharacterBase.Subscriber(Dalamud.PluginInterface);
        Reattach(attach);
    }

    private void Reattach()
        => Reattach(Glamourer.Config.AttachToPenumbra);

    public void Reattach(bool attach)
    {
        try
        {
            Unattach();

            var (breaking, feature) = Ipc.ApiVersions.Subscriber(Dalamud.PluginInterface).Invoke();
            if (breaking != RequiredPenumbraBreakingVersion || feature < RequiredPenumbraFeatureVersion)
                throw new Exception(
                    $"Invalid Version {breaking}.{feature:D4}, required major Version {RequiredPenumbraBreakingVersion} with feature greater or equal to {RequiredPenumbraFeatureVersion}.");

            if (!attach)
                return;

            _tooltipSubscriber.Enable();
            _clickSubscriber.Enable();
            CreatingCharacterBase.Enable();
            CreatedCharacterBase.Enable();
            _drawObjectInfo   = Ipc.GetDrawObjectInfo.Subscriber(Dalamud.PluginInterface);
            _cutsceneParent   = Ipc.GetCutsceneParentIndex.Subscriber(Dalamud.PluginInterface);
            _redrawSubscriber = Ipc.RedrawObject.Subscriber(Dalamud.PluginInterface);
            Available         = true;
            PluginLog.Debug("Glamourer attached to Penumbra.");
        }
        catch (Exception e)
        {
            PluginLog.Debug($"Could not attach to Penumbra:\n{e}");
        }
    }

    public void Unattach()
    {
        _tooltipSubscriber.Disable();
        _clickSubscriber.Disable();
        CreatingCharacterBase.Disable();
        CreatedCharacterBase.Disable();
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
        CreatingCharacterBase.Dispose();
        CreatedCharacterBase.Dispose();
        _initializedEvent.Dispose();
        _disposedEvent.Dispose();
    }

    private static void PenumbraTooltip(ChangedItemType type, uint _)
    {
        if (type == ChangedItemType.Item)
            ImGui.Text("Right click to apply to current Glamourer Set. [Glamourer]");
    }

    private static void PenumbraRightClick(MouseButton button, ChangedItemType type, uint id)
    {
        if (button != MouseButton.Right || type != ChangedItemType.Item)
            return;

        var item      = (Lumina.Excel.GeneratedSheets.Item)type.GetObject(Dalamud.GameData, id)!;
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
        => Available ? _drawObjectInfo.Invoke(drawObject).Item1 : IntPtr.Zero;

    public int CutsceneParent(int idx)
        => Available ? _cutsceneParent.Invoke(idx) : -1;

    public void RedrawObject(GameObject? actor, RedrawType settings)
    {
        if (actor == null || !Available)
            return;

        try
        {
            _redrawSubscriber.Invoke(actor, settings);
        }
        catch (Exception e)
        {
            PluginLog.Debug($"Failure redrawing object:\n{e}");
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
