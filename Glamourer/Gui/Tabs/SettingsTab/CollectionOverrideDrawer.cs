using Dalamud.Interface;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using ImSharp;
using Luna;
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
        using var header = Im.Tree.HeaderId("Collection Overrides"u8);
        Im.Tooltip.OnHover(
            "Here you can set up overrides for Penumbra collections that should have their settings changed when automatically applying mod settings from a design.\n"u8
          + "Instead of the collection associated with the overridden character, the overridden collection will be manipulated."u8);
        if (!header)
            return;

        using var table = Im.Table.Begin("table"u8, 4, TableFlags.RowBackground);
        if (!table)
            return;

        table.SetupColumn("buttons"u8,     TableColumnFlags.WidthFixed,   Im.Style.FrameHeight);
        table.SetupColumn("identifiers"u8, TableColumnFlags.WidthStretch, 0.35f);
        table.SetupColumn("collections"u8, TableColumnFlags.WidthStretch, 0.4f);
        table.SetupColumn("name"u8,        TableColumnFlags.WidthStretch, 0.25f);

        for (var i = 0; i < collectionOverrides.Overrides.Count; ++i)
            DrawCollectionRow(table, ref i);

        DrawNewOverride(table);
    }

    private void DrawCollectionRow(in Im.TableDisposable table, ref int idx)
    {
        using var id = Im.Id.Push(idx);
        var (exists, actor, collection, name) = collectionOverrides.Fetch(idx);

        table.NextColumn();
        if (ImEx.Icon.Button(LunaStyle.DeleteIcon, "Delete this override."u8))
            collectionOverrides.DeleteOverride(idx--);

        table.NextColumn();
        DrawActorIdentifier(idx, actor);

        table.NextColumn();
        if (combo.Draw("##collection", name, "Select the overriding collection. Current GUID:", Im.ContentRegion.Available.X,
                Im.Style.TextHeight))
        {
            var (guid, _, newName) = combo.CurrentSelection;
            collectionOverrides.ChangeOverride(idx, guid, newName);
        }

        if (Im.Item.Hovered())
        {
            using var tt   = Im.Tooltip.Begin();
            using var font = Im.Font.PushMono();
            Im.Text($"    {collection}");
        }

        table.NextColumn();
        DrawCollectionName(exists, collection, name);
    }

    private void DrawCollectionName(bool exists, Guid collection, string name)
    {
        if (!exists)
        {
            Im.Text("<Does not Exist>"u8);
            if (!Im.Item.Hovered())
                return;

            using var tt1 = Im.Tooltip.Begin();
            Im.Text($"The design {name} with the GUID");
            using (Im.Font.PushMono())
            {
                Im.Text($"    {collection}");
            }

            Im.Text("does not exist in Penumbra."u8);
            return;
        }

        Im.Text(config.Ephemeral.IncognitoMode ? collection.ToString()[..8] : name);
        if (!Im.Item.Hovered())
            return;

        using var tt2 = Im.Tooltip.Begin();
        using var f   = Im.Font.PushMono();
        Im.Text($"{collection}");
    }

    private void DrawActorIdentifier(int idx, ActorIdentifier actor)
    {
        Im.Selectable(config.Ephemeral.IncognitoMode ? actor.Incognito(null) : actor.ToString());
        using (var target = Im.DragDrop.Target())
        {
            if (target.IsDropping("DraggingOverride"u8))
            {
                collectionOverrides.MoveOverride(_dragDropIndex, idx);
                _dragDropIndex = -1;
            }
        }

        using (var source = Im.DragDrop.Source())
        {
            if (source)
            {
                source.SetPayload("DraggingOverride"u8);
                Im.Text($"Reordering Override #{idx + 1}...");
                _dragDropIndex = idx;
            }
        }
    }

    private void DrawNewOverride(in Im.TableDisposable table)
    {
        var (currentId, currentName) = penumbra.CurrentCollection;
        table.NextColumn();
        if (ImEx.Icon.Button(FontAwesomeIcon.PersonCirclePlus.Icon(), "Add override for current player."u8,
                !objects.Player.Valid && currentId != Guid.Empty))
            collectionOverrides.AddOverride([objects.PlayerData.Identifier], currentId, currentName);

        table.NextColumn();
        Im.Item.SetNextWidthFull();
        if (Im.Input.Text("##newActor"u8, ref _newIdentifier, "New Identifier..."u8))
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
            : _newIdentifier.Length is 0
                ? "Please enter an identifier string first."
                : $"The identifier string {_newIdentifier} does not result in a valid identifier{(_exception == null ? "." : $":\n\n{_exception?.Message}")}";

        table.NextColumn();
        if (ImEx.Icon.Button(LunaStyle.AddObjectIcon, tt, tt[0] is not 'A'))
            collectionOverrides.AddOverride(_identifiers, currentId, currentName);

        Im.Line.SameInner();
        ImEx.Icon.DrawAligned(LunaStyle.InfoIcon, ImGuiColor.TextDisabled.Get());
        if (Im.Item.Hovered())
            ActorIdentifierFactory.WriteUserStringTooltip(false);
    }
}
