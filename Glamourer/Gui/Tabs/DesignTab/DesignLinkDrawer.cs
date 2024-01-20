using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.Designs.Links;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignLinkDrawer(DesignLinkManager _linkManager, DesignFileSystemSelector _selector, DesignCombo _combo) : IUiService
{
    private int       _dragDropIndex       = -1;
    private LinkOrder _dragDropOrder       = LinkOrder.Self;
    private int       _dragDropTargetIndex = -1;
    private LinkOrder _dragDropTargetOrder = LinkOrder.Self;

    public void Draw()
    {
        using var header = ImRaii.CollapsingHeader("Design Links");
        if (!header)
            return;

        var width = ImGui.GetContentRegionAvail().X / 2;
        DrawList(_selector.Selected!.Links.Before, LinkOrder.Before, width);
        ImGui.SameLine();
        DrawList(_selector.Selected!.Links.After, LinkOrder.After, width);

        if (_dragDropTargetIndex < 0
         || _dragDropIndex < 0)
            return;

        _linkManager.MoveDesignLink(_selector.Selected!, _dragDropIndex, _dragDropOrder, _dragDropTargetIndex, _dragDropTargetOrder);
        _dragDropIndex       = -1;
        _dragDropTargetIndex = -1;
        _dragDropOrder       = LinkOrder.Self;
        _dragDropTargetOrder = LinkOrder.Self;
    }

    private void DrawList(IReadOnlyList<DesignLink> list, LinkOrder order, float width)
    {
        using var id = ImRaii.PushId((int)order);
        using var table = ImRaii.Table("table", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter,
            new Vector2(width, list.Count * ImGui.GetFrameHeightWithSpacing()));
        if (!table)
            return;

        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        for (var i = 0; i < list.Count; ++i)
        {
            id.Push(i);

            ImGui.TableNextColumn();
            var delete = ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), buttonSize, "Delete this link.", false, true);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted($"#{i:D2}");

            var (design, flags) = list[i];
            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding();
            ImGui.Selectable(_selector.IncognitoMode ? design.Incognito : design.Name.Text);
            DrawDragDrop(design, order, i);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(flags.ToString());

            if (delete)
                _linkManager.RemoveDesignLink(_selector.Selected!, i--, order);
        }

        ImGui.TableNextColumn();
        string tt;
        bool   canAdd;
        if (_combo.Design == null)
        {
            tt     = "Select a design first.";
            canAdd = false;
        }
        else
        {
            canAdd = LinkContainer.CanAddLink(_selector.Selected!, _combo.Design, order, out var error);
            tt     = canAdd ? $"Add a link to {_combo.Design.Name}." : $"Can not add a link to {_combo.Design.Name}: {error}";
        }

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), buttonSize, tt, !canAdd, true))
            _linkManager.AddDesignLink(_selector.Selected!, _combo.Design!, order);

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        _combo.Draw(200);
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
}
