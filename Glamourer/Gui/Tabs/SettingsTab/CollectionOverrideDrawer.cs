using Dalamud.Interface;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.SettingsTab;

public class CollectionOverrideDrawer(
    CollectionOverrideService collectionOverrides,
    Configuration config,
    ActorObjectManager objects,
    ActorManager actors,
    PenumbraService penumbra,
    CollectionCombo combo) : IService
{
    private string            _newIdentifier = string.Empty;
    private ActorIdentifier[] _identifiers   = [];
    private int               _dragDropIndex = -1;
    private Exception?        _exception;

    public void Draw()
    {
        using var header = ImRaii.CollapsingHeader("Collection Overrides");
        ImGuiUtil.HoverTooltip(
            "Here you can set up overrides for Penumbra collections that should have their settings changed when automatically applying mod settings from a design.\n"
          + "Instead of the collection associated with the overridden character, the overridden collection will be manipulated.");
        if (!header)
            return;

        using var table = ImRaii.Table("table", 4, ImGuiTableFlags.RowBg);
        if (!table)
            return;

        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("buttons",     ImGuiTableColumnFlags.WidthFixed,   buttonSize.X);
        ImGui.TableSetupColumn("identifiers", ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableSetupColumn("collections", ImGuiTableColumnFlags.WidthStretch, 0.4f);
        ImGui.TableSetupColumn("name",        ImGuiTableColumnFlags.WidthStretch, 0.25f);

        for (var i = 0; i < collectionOverrides.Overrides.Count; ++i)
            DrawCollectionRow(ref i, buttonSize);

        DrawNewOverride(buttonSize);
    }

    private void DrawCollectionRow(ref int idx, Vector2 buttonSize)
    {
        using var id = ImRaii.PushId(idx);
        var (exists, actor, collection, name) = collectionOverrides.Fetch(idx);

        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), buttonSize, "Delete this override.", false, true))
            collectionOverrides.DeleteOverride(idx--);

        ImGui.TableNextColumn();
        DrawActorIdentifier(idx, actor);

        ImGui.TableNextColumn();
        if (combo.Draw("##collection", name, $"Select the overriding collection. Current GUID:", ImGui.GetContentRegionAvail().X,
                ImGui.GetTextLineHeight()))
        {
            var (guid, _, newName) = combo.CurrentSelection;
            collectionOverrides.ChangeOverride(idx, guid, newName);
        }

        if (ImGui.IsItemHovered())
        {
            using var tt   = ImRaii.Tooltip();
            using var font = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGui.TextUnformatted($"    {collection}");
        }

        ImGui.TableNextColumn();
        DrawCollectionName(exists, collection, name);
    }

    private void DrawCollectionName(bool exists, Guid collection, string name)
    {
        if (!exists)
        {
            ImGui.TextUnformatted("<Does not Exist>");
            if (!ImGui.IsItemHovered())
                return;

            using var tt1 = ImRaii.Tooltip();
            ImGui.TextUnformatted($"The design {name} with the GUID");
            using (ImRaii.PushFont(UiBuilder.MonoFont))
            {
                ImGui.TextUnformatted($"    {collection}");
            }

            ImGui.TextUnformatted("does not exist in Penumbra.");
            return;
        }

        ImGui.TextUnformatted(config.Ephemeral.IncognitoMode ? collection.ToString()[..8] : name);
        if (!ImGui.IsItemHovered())
            return;

        using var tt2 = ImRaii.Tooltip();
        using var f   = ImRaii.PushFont(UiBuilder.MonoFont);
        ImGui.TextUnformatted(collection.ToString());
    }

    private void DrawActorIdentifier(int idx, ActorIdentifier actor)
    {
        ImGui.Selectable(config.Ephemeral.IncognitoMode ? actor.Incognito(null) : actor.ToString());
        using (var target = ImRaii.DragDropTarget())
        {
            if (target.Success && ImGuiUtil.IsDropping("DraggingOverride"))
            {
                collectionOverrides.MoveOverride(_dragDropIndex, idx);
                _dragDropIndex = -1;
            }
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source)
            {
                ImGui.SetDragDropPayload("DraggingOverride", nint.Zero, 0);
                ImGui.TextUnformatted($"Reordering Override #{idx + 1}...");
                _dragDropIndex = idx;
            }
        }
    }

    private void DrawNewOverride(Vector2 buttonSize)
    {
        var (currentId, currentName) = penumbra.CurrentCollection;
        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.PersonCirclePlus.ToIconString(), buttonSize, "Add override for current player.",
                !objects.Player.Valid && currentId != Guid.Empty, true))
            collectionOverrides.AddOverride([objects.PlayerData.Identifier], currentId, currentName);

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
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

        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), buttonSize, tt, tt[0] is not 'A', true))
            collectionOverrides.AddOverride(_identifiers, currentId, currentName);

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
            ImGui.TextUnformatted(FontAwesomeIcon.InfoCircle.ToIconString());
        }

        if (ImGui.IsItemHovered())
            ActorIdentifierFactory.WriteUserStringTooltip(false);
    }
}
