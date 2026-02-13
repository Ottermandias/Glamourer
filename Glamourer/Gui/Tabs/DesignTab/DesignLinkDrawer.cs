using Dalamud.Interface;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.Links;
using ImSharp;
using Luna;

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
        if (!h.Alive)
            return;

        Im.Tooltip.OnHover(
            "Design links are links to other designs that will be applied to characters or during automation according to the rules set.\n"u8
          + "They apply from top to bottom just like automated design sets, so anything set by an earlier design will not not be set again by later designs, and order is important.\n"u8
          + "If a linked design links to other designs, they will also be applied, so circular links are prohibited."u8);
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
        using var table = Im.Table.Begin("table"u8, 3, TableFlags.RowBackground | TableFlags.BordersOuter);
        if (!table)
            return;

        table.SetupColumn("Del"u8,  TableColumnFlags.WidthFixed, Im.Style.FrameHeight);
        table.SetupColumn("Name"u8, TableColumnFlags.WidthStretch);
        table.SetupColumn("Detail"u8, TableColumnFlags.WidthFixed,
            6 * Im.Style.FrameHeight + 5 * Im.Style.ItemInnerSpacing.X);

        using var style = ImStyleDouble.ItemSpacing.Push(Im.Style.ItemInnerSpacing);
        DrawSubList(table, selector.Selected!.Links.Before, LinkOrder.Before);
        DrawSelf(table);
        DrawSubList(table, selector.Selected!.Links.After, LinkOrder.After);
        DrawNew(table);
        MoveLink();
    }

    private void DrawSelf(in Im.TableDisposable table)
    {
        using var id = Im.Id.Push((int)LinkOrder.Self);
        table.NextColumn();
        var color = colorManager.GetColor(selector.Selected!);
        using (AwesomeIcon.Font.Push())
        {
            using var c = ImGuiColor.Text.Push(color);
            Im.Cursor.FrameAlign();
            ImEx.TextRightAligned(FontAwesomeIcon.ArrowRightLong.Icon().Span);
        }

        table.NextColumn();
        using (ImGuiColor.Text.Push(color))
        {
            Im.Cursor.FrameAlign();
            Im.Selectable(config.Ephemeral.IncognitoMode ? selector.Selected!.Incognito : selector.Selected!.Name.Text);
        }

        Im.Tooltip.OnHover("Current Design"u8);
        DrawDragDrop(selector.Selected!, LinkOrder.Self, 0);
        table.NextColumn();
        using (AwesomeIcon.Font.Push())
        {
            using var c = ImGuiColor.Text.Push(color);
            Im.Cursor.FrameAlign();
            ImEx.TextRightAligned(FontAwesomeIcon.ArrowLeftLong.Icon().Span);
        }
    }

    private void DrawSubList(in Im.TableDisposable table, IReadOnlyList<DesignLink> list, LinkOrder order)
    {
        using var id = Im.Id.Push((int)order);

        for (var i = 0; i < list.Count; ++i)
        {
            id.Push(i);

            table.NextColumn();
            var delete = ImEx.Icon.Button(LunaStyle.DeleteIcon, "Delete this link."u8);
            var (design, flags) = list[i];
            table.NextColumn();

            using (ImGuiColor.Text.Push(colorManager.GetColor(design)))
            {
                Im.Cursor.FrameAlign();
                Im.Selectable(config.Ephemeral.IncognitoMode ? design.Incognito : design.Name.Text);
            }

            DrawDragDrop(design, order, i);

            table.NextColumn();
            Im.Cursor.FrameAlign();
            DrawApplicationBoxes(i, order, flags);

            if (delete)
                linkManager.RemoveDesignLink(selector.Selected!, i--, order);
        }
    }

    private void DrawNew(in Im.TableDisposable table)
    {
        table.NextColumn();
        table.NextColumn();
        combo.Draw(StringU8.Empty, Im.ContentRegion.Available.X);
        table.NextColumn();
        string ttBefore,     ttAfter;
        bool   canAddBefore, canAddAfter;
        var    design = combo.NewSelection;
        if (design is null)
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

        if (ImEx.Icon.Button(FontAwesomeIcon.ArrowCircleUp.Icon(), ttBefore, !canAddBefore))
        {
            linkManager.AddDesignLink(selector.Selected!, design!, LinkOrder.Before);
            linkManager.MoveDesignLink(selector.Selected!, selector.Selected!.Links.Before.Count - 1, LinkOrder.Before, 0, LinkOrder.Before);
        }

        Im.Line.Same();
        if (ImEx.Icon.Button(FontAwesomeIcon.ArrowCircleDown.Icon(), ttAfter, !canAddAfter))
            linkManager.AddDesignLink(selector.Selected!, design!, LinkOrder.After);
    }

    private void DrawDragDrop(Design design, LinkOrder order, int index)
    {
        using (var source = Im.DragDrop.Source())
        {
            if (source)
            {
                source.SetPayload("DraggingLink"u8);
                Im.Text($"Reordering {design.Name}...");
                _dragDropIndex = index;
                _dragDropOrder = order;
            }
        }

        using var target = Im.DragDrop.Target();
        if (!target.IsDropping("DraggingLink"u8))
            return;

        _dragDropTargetIndex = index;
        _dragDropTargetOrder = order;
    }

    private void DrawApplicationBoxes(int idx, LinkOrder order, ApplicationType current)
    {
        var newType = current;
        using (ImStyleBorder.Frame.Push(ColorId.FolderLine.Value()))
        {
            Im.Checkbox("##all"u8, ref newType, ApplicationType.All);
        }

        Im.Tooltip.OnHover("Toggle all application modes at once."u8);

        Im.Line.Same();
        Box(0);
        Im.Line.Same();
        Box(1);
        Im.Line.Same();

        Box(2);
        Im.Line.Same();
        Box(3);
        Im.Line.Same();
        Box(4);
        if (newType != current)
            linkManager.ChangeApplicationType(selector.Selected!, idx, order, newType);
        return;

        void Box(int i)
        {
            var (applicationType, description) = ApplicationTypeExtensions.Types[i];
            using var id    = Im.Id.Push((uint)applicationType);
            var       value = current.HasFlag(applicationType);
            if (Im.Checkbox(StringU8.Empty, ref value))
                newType = value ? newType | applicationType : newType & ~applicationType;
            Im.Tooltip.OnHover(description);
        }
    }
}
