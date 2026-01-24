using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.Links;
using Dalamud.Bindings.ImGui;
using ImSharp;
using Luna;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignLinkDrawer(
    DesignLinkManager linkManager,
    DesignFileSystemSelector selector,
    LinkDesignCombo combo,
    DesignColors colorManager,
    Configuration config) : IUiService
{
    private int       _dragDropIndex       = -1;
    private LinkOrder _dragDropOrder       = LinkOrder.None;
    private int       _dragDropTargetIndex = -1;
    private LinkOrder _dragDropTargetOrder = LinkOrder.None;

    public void Draw()
    {
        using var h = DesignPanelFlag.DesignLinks.Header(config);
        if (h.Disposed)
            return;

        ImGuiUtil.HoverTooltip(
            "Design links are links to other designs that will be applied to characters or during automation according to the rules set.\n"
          + "They apply from top to bottom just like automated design sets, so anything set by an earlier design will not not be set again by later designs, and order is important.\n"
          + "If a linked design links to other designs, they will also be applied, so circular links are prohibited. ");
        if (!h)
            return;

        DrawList();
    }

    private void MoveLink()
    {
        if (_dragDropTargetIndex < 0 || _dragDropIndex < 0)
            return;

        if (_dragDropOrder is LinkOrder.Self)
            switch (_dragDropTargetOrder)
            {
                case LinkOrder.Before:
                    for (var i = selector.Selected!.Links.Before.Count - 1; i >= _dragDropTargetIndex; --i)
                        linkManager.MoveDesignLink(selector.Selected!, i, LinkOrder.Before, 0, LinkOrder.After);
                    break;
                case LinkOrder.After:
                    for (var i = 0; i <= _dragDropTargetIndex; ++i)
                    {
                        linkManager.MoveDesignLink(selector.Selected!, 0, LinkOrder.After, selector.Selected!.Links.Before.Count,
                            LinkOrder.Before);
                    }

                    break;
            }
        else if (_dragDropTargetOrder is LinkOrder.Self)
            linkManager.MoveDesignLink(selector.Selected!, _dragDropIndex, _dragDropOrder, selector.Selected!.Links.Before.Count,
                LinkOrder.Before);
        else
            linkManager.MoveDesignLink(selector.Selected!, _dragDropIndex, _dragDropOrder, _dragDropTargetIndex, _dragDropTargetOrder);

        _dragDropIndex       = -1;
        _dragDropTargetIndex = -1;
        _dragDropOrder       = LinkOrder.None;
        _dragDropTargetOrder = LinkOrder.None;
    }

    private void DrawList()
    {
        using var table = ImRaii.Table("table", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter);
        if (!table)
            return;

        ImGui.TableSetupColumn("Del",  ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Detail", ImGuiTableColumnFlags.WidthFixed,
            6 * ImGui.GetFrameHeight() + 5 * ImGui.GetStyle().ItemInnerSpacing.X);

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing);
        DrawSubList(selector.Selected!.Links.Before, LinkOrder.Before);
        DrawSelf();
        DrawSubList(selector.Selected!.Links.After, LinkOrder.After);
        DrawNew();
        MoveLink();
    }

    private void DrawSelf()
    {
        using var id = ImRaii.PushId((int)LinkOrder.Self);
        ImGui.TableNextColumn();
        var color = colorManager.GetColor(selector.Selected!);
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var c = ImGuiColor.Text.Push(color);
            ImGui.AlignTextToFramePadding();
            ImGuiUtil.RightAlign(FontAwesomeIcon.ArrowRightLong.ToIconString());
        }

        ImGui.TableNextColumn();
        using (ImGuiColor.Text.Push(color))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Selectable(selector.IncognitoMode ? selector.Selected!.Incognito : selector.Selected!.Name.Text);
        }

        ImGuiUtil.HoverTooltip("Current Design");
        DrawDragDrop(selector.Selected!, LinkOrder.Self, 0);
        ImGui.TableNextColumn();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var c = ImGuiColor.Text.Push(color);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(FontAwesomeIcon.ArrowLeftLong.ToIconString());
        }
    }

    private void DrawSubList(IReadOnlyList<DesignLink> list, LinkOrder order)
    {
        using var id = ImRaii.PushId((int)order);

        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        for (var i = 0; i < list.Count; ++i)
        {
            id.Push(i);

            ImGui.TableNextColumn();
            var delete = ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), buttonSize, "Delete this link.", false, true);
            var (design, flags) = list[i];
            ImGui.TableNextColumn();

            using (ImGuiColor.Text.Push(colorManager.GetColor(design)))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Selectable(selector.IncognitoMode ? design.Incognito : design.Name.Text);
            }

            DrawDragDrop(design, order, i);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            DrawApplicationBoxes(i, order, flags);

            if (delete)
                linkManager.RemoveDesignLink(selector.Selected!, i--, order);
        }
    }

    private void DrawNew()
    {
        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        combo.Draw(ImGui.GetContentRegionAvail().X);
        ImGui.TableNextColumn();
        string ttBefore,     ttAfter;
        bool   canAddBefore, canAddAfter;
        var    design = combo.Design as Design;
        if (design == null)
        {
            ttAfter      = ttBefore    = "Select a design first.";
            canAddBefore = canAddAfter = false;
        }
        else
        {
            canAddBefore = LinkContainer.CanAddLink(selector.Selected!, design, LinkOrder.Before, out var error);
            ttBefore = canAddBefore
                ? $"Add a link at the top of the list to {design.Name}."
                : $"Can not add a link to {design.Name}:\n{error}";
            canAddAfter = LinkContainer.CanAddLink(selector.Selected!, design, LinkOrder.After, out error);
            ttAfter = canAddAfter
                ? $"Add a link at the bottom of the list to {design.Name}."
                : $"Can not add a link to {design.Name}:\n{error}";
        }

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.ArrowCircleUp.ToIconString(), buttonSize, ttBefore, !canAddBefore, true))
        {
            linkManager.AddDesignLink(selector.Selected!, design!, LinkOrder.Before);
            linkManager.MoveDesignLink(selector.Selected!, selector.Selected!.Links.Before.Count - 1, LinkOrder.Before, 0, LinkOrder.Before);
        }

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.ArrowCircleDown.ToIconString(), buttonSize, ttAfter, !canAddAfter, true))
            linkManager.AddDesignLink(selector.Selected!, design!, LinkOrder.After);
    }

    private void DrawDragDrop(Design design, LinkOrder order, int index)
    {
        using (var source = ImRaii.DragDropSource())
        {
            if (source)
            {
                ImGui.SetDragDropPayload("DraggingLink", null, 0);
                ImGui.TextUnformatted($"Reordering {design.Name}...");
                _dragDropIndex = index;
                _dragDropOrder = order;
            }
        }

        using var target = ImRaii.DragDropTarget();
        if (!target)
            return;

        if (!ImGuiUtil.IsDropping("DraggingLink"))
            return;

        _dragDropTargetIndex = index;
        _dragDropTargetOrder = order;
    }

    private void DrawApplicationBoxes(int idx, LinkOrder order, ApplicationType current)
    {
        var newType    = current;
        var newTypeInt = (uint)newType;
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, ImGuiHelpers.GlobalScale))
        {
            using var _ = ImRaii.PushColor(ImGuiCol.Border, ColorId.FolderLine.Value());
            if (ImGui.CheckboxFlags("##all", ref newTypeInt, (uint)ApplicationType.All))
                newType = (ApplicationType)newTypeInt;
        }

        ImGuiUtil.HoverTooltip("Toggle all application modes at once.");

        ImGui.SameLine();
        Box(0);
        ImGui.SameLine();
        Box(1);
        ImGui.SameLine();

        Box(2);
        ImGui.SameLine();
        Box(3);
        ImGui.SameLine();
        Box(4);
        if (newType != current)
            linkManager.ChangeApplicationType(selector.Selected!, idx, order, newType);
        return;

        void Box(int i)
        {
            var (applicationType, description) = ApplicationTypeExtensions.Types[i];
            var value = current.HasFlag(applicationType);
            if (ImGui.Checkbox($"##{(byte)applicationType}", ref value))
                newType = value ? newType | applicationType : newType & ~applicationType;
            ImGuiUtil.HoverTooltip(description);
        }
    }
}
