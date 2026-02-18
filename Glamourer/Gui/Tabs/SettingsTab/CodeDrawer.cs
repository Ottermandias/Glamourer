using Dalamud.Interface;
using Glamourer.Services;
using Glamourer.State;
using ImSharp;
using Luna;
using OtterGui.Filesystem;
using OtterGui.Raii;
using OtterGui.Text;
using OtterGui.Text.EndObjects;

namespace Glamourer.Gui.Tabs.SettingsTab;

public class CodeDrawer(Configuration.Configuration config, CodeService codeService, FunModule funModule) : IUiService
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
        var show = Im.Tree.Header("Cheat Codes"u8);
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
        using var border = ImRaii.PushFrameBorder(Im.Style.GlobalScale, color.Value().Color, _currentCode.Length > 0);
        Im.Item.SetNextWidth(500 * Im.Style.GlobalScale + Im.Style.ItemSpacing.X);
        if (Im.Input.Text("##Code"u8, ref _currentCode, "Enter Cheat Code..."u8, InputTextFlags.EnterReturnsTrue))
        {
            codeService.AddCode(_currentCode);
            _currentCode = string.Empty;
        }

        Im.Line.Same();
        ImUtf8.Icon(FontAwesomeIcon.ExclamationCircle, ImGuiColor.TextDisabled.Get().Color);
        DrawTooltip();
    }

    private void DrawCopyButtons()
    {
        var buttonSize = new Vector2(250 * Im.Style.GlobalScale, 0);
        if (ImUtf8.Button("Who am I?!?"u8, buttonSize))
            funModule.WhoAmI();
        ImUtf8.HoverTooltip(
            "Copy your characters actual current appearance including cheat codes or holiday events to the clipboard as a design."u8);

        Im.Line.Same();

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
                    disabled: !canDelete))
            {
                action!(false);
                config.Codes.RemoveAt(i--);
                codeService.SaveState();
            }

            knownFlags |= flag;
            Im.Line.SameInner();
            if (ImUtf8.Checkbox("\0"u8, ref state))
            {
                action!(state);
                codeService.SaveState();
            }

            var hovered = Im.Item.Hovered();
            Im.Line.Same();
            ImUtf8.Selectable(code);
            hovered |= Im.Item.Hovered();
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

        if (Extensions.Move(config.Codes, _dragCodeIdx, idx))
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

        foreach (var code in CodeService.CodeFlag.Values)
        {
            if (knownFlags.HasFlag(code))
                continue;

            var data = CodeService.GetData(code);
            if (!data.Display)
                continue;

            Im.Dummy(Vector2.Zero);
            Im.Separator();
            Im.Dummy(Vector2.Zero);
            ImUtf8.Text(data.Effect);
            using var indent = ImRaii.PushIndent(2);
            using (ImUtf8.Group())
            {
                ImUtf8.Text("Capitalized letters: "u8);
                ImUtf8.Text("Punctuation: "u8);
            }

            Im.Line.SameInner();
            using (ImUtf8.Group())
            {
                using var mono = Im.Font.PushMono();
                ImUtf8.Text($"{data.CapitalCount}");
                ImUtf8.Text($"{data.Punctuation}");
            }

            ImUtf8.TextWrapped(data.Hint);
        }
    }


    private static void DrawTooltip()
    {
        if (!Im.Item.Hovered())
            return;

        Im.Window.SetNextSize(new Vector2(400, 0));
        using var tt = ImUtf8.Tooltip();
        ImUtf8.TextWrapped(Tooltip);
    }
}
