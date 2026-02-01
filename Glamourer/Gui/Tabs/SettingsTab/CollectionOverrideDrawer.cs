using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using ImSharp;
using Luna;
using OtterGui;
using OtterGui.Raii;
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

        using var table = Im.Table.Begin("table"u8, 4, TableFlags.RowBackground);
        if (!table)
            return;

        var buttonSize = new Vector2(Im.Style.FrameHeight);
        table.SetupColumn("buttons"u8,     TableColumnFlags.WidthFixed,   buttonSize.X);
        table.SetupColumn("identifiers"u8, TableColumnFlags.WidthStretch, 0.35f);
        table.SetupColumn("collections"u8, TableColumnFlags.WidthStretch, 0.4f);
        table.SetupColumn("name"u8,        TableColumnFlags.WidthStretch, 0.25f);

        for (var i = 0; i < collectionOverrides.Overrides.Count; ++i)
            DrawCollectionRow(ref i, buttonSize);

        DrawNewOverride(buttonSize);
    }

    private void DrawCollectionRow(ref int idx, Vector2 buttonSize)
    {
        using var id = Im.Id.Push(idx);
        var (exists, actor, collection, name) = collectionOverrides.Fetch(idx);

        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), buttonSize, "Delete this override.", false, true))
            collectionOverrides.DeleteOverride(idx--);

        ImGui.TableNextColumn();
        DrawActorIdentifier(idx, actor);

        ImGui.TableNextColumn();
        if (combo.Draw("##collection", name, $"Select the overriding collection. Current GUID:", Im.ContentRegion.Available.X,
                Im.Style.TextHeight))
        {
            var (guid, _, newName) = combo.CurrentSelection;
            collectionOverrides.ChangeOverride(idx, guid, newName);
        }

        if (ImGui.IsItemHovered())
        {
            using var tt   = ImRaii.Tooltip();
            using var font = Im.Font.PushMono();
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
            using (Im.Font.PushMono())
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
        using var f   = Im.Font.PushMono();
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
                ImGui.SetDragDropPayload("DraggingOverride", null, 0);
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
        ImGui.SetNextItemWidth(Im.ContentRegion.Available.X);
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

        ImGui.SameLine(0, Im.Style.ItemInnerSpacing.X);
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var color = ImGuiColor.Text.Push(Im.Style[ImGuiColor.TextDisabled]);
            ImGui.TextUnformatted(FontAwesomeIcon.InfoCircle.ToIconString());
        }

        if (ImGui.IsItemHovered())
            ActorIdentifierFactory.WriteUserStringTooltip(false);
    }
}
