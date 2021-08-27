using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin;
using Glamourer.Gui;
using ImGuiNET;
using Penumbra.Api;
using Penumbra.PlayerWatch;

namespace Glamourer
{
    public class PenumbraAttach : IDisposable
    {
        public const int RequiredPenumbraShareVersion = 3;

        private ICallGateSubscriber<object?, object>?         TooltipSubscriber;
        private ICallGateSubscriber<int, object?, object>?    ClickSubscriber;
        private ICallGateSubscriber<string, int, object>?     RedrawSubscriberName;
        private ICallGateSubscriber<GameObject, int, object>? RedrawSubscriberObject;

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

                RedrawSubscriberName   = Dalamud.PluginInterface.GetIpcSubscriber<string, int, object>("Penumbra.RedrawObjectByName");
                RedrawSubscriberObject = Dalamud.PluginInterface.GetIpcSubscriber<GameObject, int, object>("Penumbra.RedrawObject");

                if (!attach)
                    return;

                TooltipSubscriber = Dalamud.PluginInterface.GetIpcSubscriber<object?, object>("Penumbra.ChangedItemTooltip");
                ClickSubscriber   = Dalamud.PluginInterface.GetIpcSubscriber<int, object?, object>("Penumbra.ChangedItemClick");
                TooltipSubscriber.Subscribe(PenumbraTooltip);
                ClickSubscriber.Subscribe(PenumbraRightClickWrapper);
            }
            catch (Exception e)
            {
                PluginLog.Debug($"Could not attach to Penumbra:\n{e}");
            }
        }

        public void Unattach()
        {
            TooltipSubscriber?.Unsubscribe(PenumbraTooltip);
            ClickSubscriber?.Unsubscribe(PenumbraRightClickWrapper);
            TooltipSubscriber      = null;
            ClickSubscriber        = null;
            RedrawSubscriberName   = null;
            RedrawSubscriberObject = null;
        }

        public void Dispose()
            => Unattach();

        private static void PenumbraTooltip(object? it)
        {
            if (it is Lumina.Excel.GeneratedSheets.Item)
                ImGui.Text("Right click to apply to current Glamourer Set. [Glamourer]");
        }

        private void PenumbraRightClick(MouseButton button, object? it)
        {
            if (button != MouseButton.Right || it is not Lumina.Excel.GeneratedSheets.Item item)
                return;

            var gPose     = Dalamud.Objects[Interface.GPoseActorId] as Character;
            var player    = Dalamud.Objects[0] as Character;
            var writeItem = new Item(item, string.Empty);
            if (gPose != null)
            {
                writeItem.Write(gPose.Address);
                UpdateActors(gPose, player);
            }
            else if (player != null)
            {
                writeItem.Write(player.Address);
                UpdateActors(player);
            }
        }

        private void PenumbraRightClickWrapper(int button, object? it)
            => PenumbraRightClick((MouseButton) button, it);

        public void RedrawObject(GameObject actor, RedrawType settings, bool repeat)
        {
            if (RedrawSubscriberObject != null)
            {
                try
                {
                    RedrawSubscriberObject.InvokeFunc(actor, (int) settings);
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
                        PluginLog.Debug($"Failure redrawing actor:\n{e}");
                    }
                }
            }
            else if (repeat)
            {
                Reattach(Glamourer.Config.AttachToPenumbra);
                RedrawObject(actor, settings, false);
            }
            else
                PluginLog.Debug("Trying to redraw actor, but not attached to Penumbra.");
        }

        // Update actors without triggering PlayerWatcher Events,
        // then manually redraw using Penumbra.
        public void UpdateActors(Character actor, Character? gPoseOriginalActor = null)
        {
            var newEquip = Glamourer.PlayerWatcher.UpdateActorWithoutEvent(actor);
            RedrawObject(actor, RedrawType.WithSettings, true);

            // Special case for carrying over changes to the gPose actor to the regular player actor, too.
            if (gPoseOriginalActor == null)
                return;

            newEquip.Write(gPoseOriginalActor.Address);
            Glamourer.PlayerWatcher.UpdateActorWithoutEvent(gPoseOriginalActor);
            RedrawObject(gPoseOriginalActor, RedrawType.AfterGPoseWithSettings, false);
        }
    }
}
