using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin.Ipc;
using Glamourer.Gui;
using ImGuiNET;
using Penumbra.GameData.Enums;

namespace Glamourer
{
    public class PenumbraAttach : IDisposable
    {
        public const int RequiredPenumbraShareVersion = 3;

        private ICallGateSubscriber<ChangedItemType, uint, object>?              _tooltipSubscriber;
        private ICallGateSubscriber<MouseButton, ChangedItemType, uint, object>? _clickSubscriber;
        private ICallGateSubscriber<string, int, object>?                        _redrawSubscriberName;
        private ICallGateSubscriber<GameObject, int, object>?                    _redrawSubscriberObject;

        public PenumbraAttach(bool attach)
            => Reattach(attach);

        public void Reattach(bool attach)
        {
            try
            {
                Unattach();

                var versionSubscriber = Dalamud.PluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion");
                var version           = versionSubscriber.InvokeFunc();
                if (version != RequiredPenumbraShareVersion)
                    throw new Exception($"Invalid Version {version}, required Version {RequiredPenumbraShareVersion}.");

                _redrawSubscriberName   = Dalamud.PluginInterface.GetIpcSubscriber<string, int, object>("Penumbra.RedrawObjectByName");
                _redrawSubscriberObject = Dalamud.PluginInterface.GetIpcSubscriber<GameObject, int, object>("Penumbra.RedrawObject");

                if (!attach)
                    return;

                _tooltipSubscriber = Dalamud.PluginInterface.GetIpcSubscriber<ChangedItemType, uint, object>("Penumbra.ChangedItemTooltip");
                _clickSubscriber =
                    Dalamud.PluginInterface.GetIpcSubscriber<MouseButton, ChangedItemType, uint, object>("Penumbra.ChangedItemClick");
                _tooltipSubscriber.Subscribe(PenumbraTooltip);
                _clickSubscriber.Subscribe(PenumbraRightClick);
            }
            catch (Exception e)
            {
                PluginLog.Debug($"Could not attach to Penumbra:\n{e}");
            }
        }

        public void Unattach()
        {
            _tooltipSubscriber?.Unsubscribe(PenumbraTooltip);
            _clickSubscriber?.Unsubscribe(PenumbraRightClick);
            _tooltipSubscriber      = null;
            _clickSubscriber        = null;
            _redrawSubscriberName   = null;
            _redrawSubscriberObject = null;
        }

        public void Dispose()
            => Unattach();

        private static void PenumbraTooltip(ChangedItemType type, uint _)
        {
            if (type == ChangedItemType.Item)
                ImGui.Text("Right click to apply to current Glamourer Set. [Glamourer]");
        }

        private void PenumbraRightClick(MouseButton button, ChangedItemType type, uint id)
        {
            if (button != MouseButton.Right || type != ChangedItemType.Item)
                return;

            var gPose     = Dalamud.Objects[Interface.GPoseObjectId] as Character;
            var player    = Dalamud.Objects[0] as Character;
            var item      = (Lumina.Excel.GeneratedSheets.Item) type.GetObject(id)!;
            var writeItem = new Item(item, string.Empty);
            if (gPose != null)
            {
                writeItem.Write(gPose.Address);
                UpdateCharacters(gPose, player);
            }
            else if (player != null)
            {
                writeItem.Write(player.Address);
                UpdateCharacters(player);
            }
        }

        public void RedrawObject(GameObject actor, RedrawType settings, bool repeat)
        {
            if (_redrawSubscriberObject != null)
            {
                try
                {
                    _redrawSubscriberObject.InvokeAction(actor, (int) settings);
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
            var newEquip = Glamourer.PlayerWatcher.UpdatePlayerWithoutEvent(character);
            RedrawObject(character, RedrawType.WithSettings, true);

            // Special case for carrying over changes to the gPose player to the regular player, too.
            if (gPoseOriginalCharacter == null)
                return;

            newEquip.Write(gPoseOriginalCharacter.Address);
            Glamourer.PlayerWatcher.UpdatePlayerWithoutEvent(gPoseOriginalCharacter);
            RedrawObject(gPoseOriginalCharacter, RedrawType.AfterGPoseWithSettings, false);
        }
    }
}
