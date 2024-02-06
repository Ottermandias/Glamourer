using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.Links;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignLinkDrawer(DesignLinkManager _linkManager, DesignFileSystemSelector _selector, LinkDesignCombo _combo) : IUiService
{
    private int       _dragDropIndex       = -1;
    private LinkOrder _dragDropOrder       = LinkOrder.None;
    private int       _dragDropTargetIndex = -1;
    private LinkOrder _dragDropTargetOrder = LinkOrder.None;

    public void Draw()
    {
        using var header = ImRaii.CollapsingHeader("Design Links");
        if (!header)
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
                    for (var i = _selector.Selected!.Links.Before.Count - 1; i >= _dragDropTargetIndex; --i)
                        _linkManager.MoveDesignLink(_selector.Selected!, i, LinkOrder.Before, 0, LinkOrder.After);
                    break;
                case LinkOrder.After:
                    for (var i = 0; i <= _dragDropTargetIndex; ++i)
                    {
                        _linkManager.MoveDesignLink(_selector.Selected!, 0, LinkOrder.After, _selector.Selected!.Links.Before.Count,
                            LinkOrder.Before);
                    }

                    break;
            }
        else if (_dragDropTargetOrder is LinkOrder.Self)
            _linkManager.MoveDesignLink(_selector.Selected!, _dragDropIndex, _dragDropOrder, _selector.Selected!.Links.Before.Count,
                LinkOrder.Before);
        else
            _linkManager.MoveDesignLink(_selector.Selected!, _dragDropIndex, _dragDropOrder, _dragDropTargetIndex, _dragDropTargetOrder);

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
        DrawSubList(_selector.Selected!.Links.Before, LinkOrder.Before);
        DrawSelf();
        DrawSubList(_selector.Selected!.Links.After, LinkOrder.After);
        DrawNew();
        MoveLink();
    }

    private void DrawSelf()
    {
        using var id = ImRaii.PushId((int)LinkOrder.Self);
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Selectable(_selector.IncognitoMode ? _selector.Selected!.Incognito : _selector.Selected!.Name.Text);
        DrawDragDrop(_selector.Selected!, LinkOrder.Self, 0);
        ImGui.TableNextColumn();
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

            ImGui.AlignTextToFramePadding();
            ImGui.Selectable(_selector.IncognitoMode ? design.Incognito : design.Name.Text);
            DrawDragDrop(design, order, i);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            DrawApplicationBoxes(i, order, flags);

            if (delete)
                _linkManager.RemoveDesignLink(_selector.Selected!, i--, order);
        }
    }

    private void DrawNew()
    {
        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        _combo.Draw(ImGui.GetContentRegionAvail().X);
        ImGui.TableNextColumn();
        string ttBefore,     ttAfter;
        bool   canAddBefore, canAddAfter;
        if (_combo.Design == null)
        {
            ttAfter      = ttBefore    = "Select a design first.";
            canAddBefore = canAddAfter = false;
        }
        else
        {
            canAddBefore = LinkContainer.CanAddLink(_selector.Selected!, _combo.Design, LinkOrder.Before, out var error);
            ttBefore = canAddBefore
                ? $"Add a link at the top of the list to {_combo.Design.Name}."
                : $"Can not add a link to {_combo.Design.Name}:\n{error}";
            canAddAfter = LinkContainer.CanAddLink(_selector.Selected!, _combo.Design, LinkOrder.After, out error);
            ttAfter = canAddAfter
                ? $"Add a link at the bottom of the list to {_combo.Design.Name}."
                : $"Can not add a link to {_combo.Design.Name}:\n{error}";
        }

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.ArrowCircleUp.ToIconString(), buttonSize, ttBefore, !canAddBefore, true))
        {
            _linkManager.AddDesignLink(_selector.Selected!, _combo.Design!, LinkOrder.Before);
            _linkManager.MoveDesignLink(_selector.Selected!, _selector.Selected!.Links.Before.Count - 1, LinkOrder.Before, 0, LinkOrder.Before);
        }

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.ArrowCircleDown.ToIconString(), buttonSize, ttAfter, !canAddAfter, true))
            _linkManager.AddDesignLink(_selector.Selected!, _combo.Design!, LinkOrder.After);
    }

    private void DrawDragDrop(Design design, LinkOrder order, int index)
    {
        using (var source = ImRaii.DragDropSource())
        {
            if (source)
            {
                ImGui.SetDragDropPayload("DraggingLink", IntPtr.Zero, 0);
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
            _linkManager.ChangeApplicationType(_selector.Selected!, idx, order, current);
        return;

        void Box(int i)
        {
            var (applicationType, description) = ApplicationTypeExtensions.Types[i];
            var value = applicationType.HasFlag(applicationType);
            if (ImGui.Checkbox($"##{(byte)applicationType}", ref value))
                newType = value ? newType | applicationType : newType & ~applicationType;
            ImGuiUtil.HoverTooltip(description);
        }
    }
}
