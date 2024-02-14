using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Style;
using Glamourer.Interop;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;
using Penumbra.GameData.Actors;

namespace Glamourer.Gui.Tabs.SettingsTab;

public class CollectionOverrideDrawer(
    CollectionOverrideService collectionOverrides,
    Configuration config,
    ObjectManager objects,
    ActorManager actors) : IService
{
    private string            _newIdentifier = string.Empty;
    private ActorIdentifier[] _identifiers   = [];
    private int               _dragDropIndex = -1;
    private Exception?        _exception;
    private string            _collection = string.Empty;

    public void Draw()
    {
        using var header = ImRaii.CollapsingHeader("Collection Overrides");
        ImGuiUtil.HoverTooltip(
            "Here you can set up overrides for Penumbra collections that should have their settings changed when automatically applying mod settings from a design.\n"
          + "Instead of the collection associated with the overridden character, the overridden collection will be manipulated.");
        if (!header)
            return;

        using var table = ImRaii.Table("table", 3, ImGuiTableFlags.RowBg);
        if (!table)
            return;

        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("buttons",     ImGuiTableColumnFlags.WidthFixed,   buttonSize.X);
        ImGui.TableSetupColumn("identifiers", ImGuiTableColumnFlags.WidthStretch, 0.6f);
        ImGui.TableSetupColumn("collections", ImGuiTableColumnFlags.WidthStretch, 0.4f);

        for (var i = 0; i < collectionOverrides.Overrides.Count; ++i)
        {
            var (identifier, collection) = collectionOverrides.Overrides[i];
            using var id = ImRaii.PushId(i);
            ImGui.TableNextColumn();
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), buttonSize, "Delete this override.", false, true))
                collectionOverrides.DeleteOverride(i--);

            ImGui.TableNextColumn();
            ImGui.Selectable(config.Ephemeral.IncognitoMode ? identifier.Incognito(null) : identifier.ToString());

            using (var target = ImRaii.DragDropTarget())
            {
                if (target.Success && ImGuiUtil.IsDropping("DraggingOverride"))
                {
                    collectionOverrides.MoveOverride(_dragDropIndex, i);
                    _dragDropIndex = -1;
                }
            }

            using (var source = ImRaii.DragDropSource())
            {
                if (source)
                {
                    ImGui.SetDragDropPayload("DraggingOverride", nint.Zero, 0);
                    ImGui.TextUnformatted($"Reordering Override #{i + 1}...");
                    _dragDropIndex = i;
                }
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.InputText("##input", ref collection, 64) && collection.Length > 0)
                collectionOverrides.ChangeOverride(i, collection);
        }

        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.PersonCirclePlus.ToIconString(), buttonSize, "Add override for current player.",
                !objects.Player.Valid, true))
            collectionOverrides.AddOverride([objects.PlayerData.Identifier], _collection.Length > 0 ? _collection : "TempCollection");

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X - buttonSize.X * 2);
        if (ImGui.InputTextWithHint("##newActor", "New Identifier...", ref _newIdentifier, 80))
            try
            {
                _identifiers = actors.FromUserString(_newIdentifier, false);
            }
            catch (ActorIdentifierFactory.IdentifierParseError e)
            {
                _exception   = e;
                _identifiers = [];
            }

        var tt = _identifiers.Any(i => i.IsValid)
            ? $"Add a new override for {_identifiers.First(i => i.IsValid)}."
            : _newIdentifier.Length == 0
                ? "Please enter an identifier string first."
                : $"The identifier string {_newIdentifier} does not result in a valid identifier{(_exception == null ? "." : $":\n\n{_exception?.Message}")}";

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), buttonSize, tt, tt[0] is 'T', true))
            collectionOverrides.AddOverride(_identifiers, _collection.Length > 0 ? _collection : "TempCollection");

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
            ImGui.TextUnformatted(FontAwesomeIcon.InfoCircle.ToIconString());
        }

        if (ImGui.IsItemHovered())
            ActorIdentifierFactory.WriteUserStringTooltip(false);

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##collection", "Enter Collection...", ref _collection, 80);
    }
}
