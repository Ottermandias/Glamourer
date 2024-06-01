using Dalamud.Interface;
using Glamourer.Services;
using Glamourer.State;
using ImGuiNET;
using OtterGui.Filesystem;
using OtterGui.Raii;
using OtterGui.Services;
using OtterGui.Text;
using OtterGui.Text.EndObjects;

namespace Glamourer.Gui.Tabs.SettingsTab;

public class CodeDrawer(Configuration config, CodeService codeService, FunModule funModule) : IUiService
{
    private static ReadOnlySpan<byte> Tooltip
        => "Cheat Codes are not actually for cheating in the game, but for 'cheating' in Glamourer. "u8
          + "They allow for some fun easter-egg modes that usually manipulate the appearance of all players you see (including yourself) in some way."u8;

    private static ReadOnlySpan<byte> DragDropLabel
        => "##CheatDrag"u8;

    private bool   _showCodeHints;
    private string _currentCode = string.Empty;
    private int    _dragCodeIdx = -1;


    public void Draw()
    {
        var show = ImGui.CollapsingHeader("Cheat Codes");
        DrawTooltip();

        if (!show)
            return;

        DrawCodeInput();
        DrawCopyButtons();
        var knownFlags = DrawCodes();
        DrawCodeHints(knownFlags);
    }

    private void DrawCodeInput()
    {
        var       color  = codeService.CheckCode(_currentCode).Item2 is not 0 ? ColorId.ActorAvailable : ColorId.ActorUnavailable;
        using var border = ImRaii.PushFrameBorder(ImUtf8.GlobalScale, color.Value(), _currentCode.Length > 0);
        ImGui.SetNextItemWidth(500 * ImUtf8.GlobalScale + ImUtf8.ItemSpacing.X);
        if (ImUtf8.InputText("##Code"u8, ref _currentCode, "Enter Cheat Code..."u8, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            codeService.AddCode(_currentCode);
            _currentCode = string.Empty;
        }

        ImGui.SameLine();
        ImUtf8.Icon(FontAwesomeIcon.ExclamationCircle, ImGui.GetColorU32(ImGuiCol.TextDisabled));
        DrawTooltip();
    }

    private void DrawCopyButtons()
    {
        var buttonSize = new Vector2(250 * ImUtf8.GlobalScale, 0);
        if (ImUtf8.Button("Who am I?!?"u8, buttonSize))
            funModule.WhoAmI();
        ImUtf8.HoverTooltip(
            "Copy your characters actual current appearance including cheat codes or holiday events to the clipboard as a design."u8);

        ImGui.SameLine();

        if (ImUtf8.Button("Who is that!?!"u8, buttonSize))
            funModule.WhoIsThat();
        ImUtf8.HoverTooltip(
            "Copy your targets actual current appearance including cheat codes or holiday events to the clipboard as a design."u8);
    }

    private CodeService.CodeFlag DrawCodes()
    {
        var                  canDelete  = config.DeleteDesignModifier.IsActive();
        CodeService.CodeFlag knownFlags = 0;
        for (var i = 0; i < config.Codes.Count; ++i)
        {
            using var id = ImUtf8.PushId(i);
            var (code, state)  = config.Codes[i];
            var (action, flag) = codeService.CheckCode(code);
            if (flag is 0)
                continue;

            var data = CodeService.GetData(flag);

            if (ImUtf8.IconButton(FontAwesomeIcon.Trash,
                    $"Delete this cheat code.{(canDelete ? string.Empty : $"\nHold {config.DeleteDesignModifier} while clicking to delete.")}",
                    !canDelete))
            {
                action!(false);
                config.Codes.RemoveAt(i--);
                codeService.SaveState();
            }

            knownFlags |= flag;
            ImUtf8.SameLineInner();
            if (ImUtf8.Checkbox("\0"u8, ref state))
            {
                action!(state);
                codeService.SaveState();
            }

            var hovered = ImGui.IsItemHovered();
            ImGui.SameLine();
            ImUtf8.Selectable(code, false);
            hovered |= ImGui.IsItemHovered();
            DrawSource(i, code);
            DrawTarget(i);
            if (hovered)
            {
                using var tt = ImUtf8.Tooltip();
                ImUtf8.Text(data.Effect);
            }
        }

        return knownFlags;
    }

    private void DrawSource(int idx, string code)
    {
        using var source = ImUtf8.DragDropSource();
        if (!source)
            return;

        if (!DragDropSource.SetPayload(DragDropLabel))
            _dragCodeIdx = idx;
        ImUtf8.Text($"Dragging {code}...");
    }

    private void DrawTarget(int idx)
    {
        using var target = ImUtf8.DragDropTarget();
        if (!target.IsDropping(DragDropLabel) || _dragCodeIdx == -1)
            return;

        if (config.Codes.Move(_dragCodeIdx, idx))
            codeService.SaveState();
        _dragCodeIdx = -1;
    }

    private void DrawCodeHints(CodeService.CodeFlag knownFlags)
    {
        if (knownFlags.HasFlag(CodeService.AllHintCodes))
            return;

        if (ImUtf8.Button(_showCodeHints ? "Hide Hints"u8 : "Show Hints"u8))
            _showCodeHints = !_showCodeHints;

        if (!_showCodeHints)
            return;

        foreach (var code in Enum.GetValues<CodeService.CodeFlag>())
        {
            if (knownFlags.HasFlag(code))
                continue;

            var data = CodeService.GetData(code);
            if (!data.Display)
                continue;

            ImGui.Dummy(Vector2.Zero);
            ImGui.Separator();
            ImGui.Dummy(Vector2.Zero);
            ImUtf8.Text(data.Effect);
            using var indent = ImRaii.PushIndent(2);
            using (ImUtf8.Group())
            {
                ImUtf8.Text("Capitalized letters: "u8);
                ImUtf8.Text("Punctuation: "u8);
            }

            ImUtf8.SameLineInner();
            using (ImUtf8.Group())
            {
                using var mono = ImRaii.PushFont(UiBuilder.MonoFont);
                ImUtf8.Text($"{data.CapitalCount}");
                ImUtf8.Text($"{data.Punctuation}");
            }

            ImUtf8.TextWrapped(data.Hint);
        }
    }


    private static void DrawTooltip()
    {
        if (!ImGui.IsItemHovered())
            return;

        ImGui.SetNextWindowSize(new Vector2(400, 0));
        using var tt = ImUtf8.Tooltip();
        ImUtf8.TextWrapped(Tooltip);
    }
}
