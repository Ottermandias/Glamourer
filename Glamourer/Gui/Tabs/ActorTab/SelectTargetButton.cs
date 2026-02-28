using Dalamud.Interface;
using ImSharp;
using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class SelectTargetButton(ActorObjectManager objects, ActorSelection selection) : BaseIconButton<AwesomeIcon>, IUiService
{
    public override AwesomeIcon Icon
        => FontAwesomeIcon.HandPointer;

    public override void DrawTooltip()
    {
        var (id, data) = objects.TargetData;
        if (data.Valid && data.Objects[0].Model.IsCharacterBase)
            Im.Text($"Select the current target {id} in the list.");
        else
            Im.Text("No valid target selected."u8);
    }

    public override bool HasTooltip
        => true;

    public override bool Enabled
        => objects is { IsInGPose: false, TargetData.Data.Valid: true };

    public override void OnClick()
    {
        var (identifier, data) = objects.TargetData;
        selection.Select(identifier, data);
    }
}
