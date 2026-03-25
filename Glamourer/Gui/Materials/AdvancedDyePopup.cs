using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Interop.Material;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Interop;
using Penumbra.String;
using Notification = Luna.Notification;

namespace Glamourer.Gui.Materials;

public sealed unsafe class AdvancedDyePopup(
    Configuration config,
    StateManager stateManager,
    LiveColorTablePreviewer preview,
    DirectXService directX) : IService
{
    public static readonly AwesomeIcon Palette = FontAwesomeIcon.Palette;

    private MaterialValueIndex? _drawIndex;
    private ActorState          _state = null!;
    private Actor               _actor;
    private ColorRow.Mode       _mode;
    private byte                _selectedMaterial = byte.MaxValue;
    private bool                _anyChanged;
    private bool                _forceFocus;

    private const int  RowsPerPage = 16;
    private       int  _rowOffset;
    private       bool _editSheen;

    private bool ShouldBeDrawn()
    {
        if (_drawIndex is not { Valid: true })
            return false;

        if (!_actor.IsCharacter || !_state.ModelData.IsHuman || !_actor.Model.IsHuman)
            return false;

        return true;
    }

    public void DrawButton(EquipSlot slot, ColorParameter color)
        => DrawButton(MaterialValueIndex.FromSlot(slot), color);

    public void DrawButton(BonusItemFlag slot, ColorParameter color)
        => DrawButton(MaterialValueIndex.FromSlot(slot), color);

    private void DrawButton(MaterialValueIndex index, ColorParameter color)
    {
        if (config.HideDesignPanel.HasFlag(DesignPanelFlag.AdvancedDyes))
            return;

        Im.Line.Same();
        using var id     = Im.Id.Push(index.SlotIndex | ((int)index.DrawObject << 8));
        var       isOpen = index == _drawIndex;

        var (textColor, buttonColor) = isOpen
            ? (ColorId.HeaderButtons.Value(), ImGuiColor.ButtonActive.Get())
            : (color, ColorParameter.Default);

        using (ImStyleBorder.Frame.Push(textColor, 2 * Im.Style.GlobalScale, isOpen))
        {
            if (ImEx.Icon.Button(Palette, StringU8.Empty, false, buttonColor, textColor))
            {
                _forceFocus       = true;
                _selectedMaterial = byte.MaxValue;
                _drawIndex        = isOpen ? null : index;
            }
        }

        Im.Tooltip.OnHover("Open advanced dyes for this slot."u8);
    }

    private (string Path, string GamePath) ResourceName(MaterialValueIndex index)
    {
        var materialHandle =
            (MaterialResourceHandle*)_actor.Model.AsCharacterBase->MaterialsSpan[
                index.MaterialIndex + index.SlotIndex * MaterialService.MaterialsPerModel].Value;
        var model       = _actor.Model.AsCharacterBase->ModelsSpan[index.SlotIndex].Value;
        var modelHandle = model == null ? null : model->ModelResourceHandle;
        var path = materialHandle == null
            ? string.Empty
            : ByteString.FromSpanUnsafe(materialHandle->FileName.AsSpan(), true).ToString();
        var gamePath = modelHandle == null
            ? string.Empty
            : modelHandle->GetMaterialFileNameBySlot(index.MaterialIndex).ToString();
        return (path, gamePath);
    }

    private void DrawTabBar(ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<Texture>> textures,
        ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<Material>> materials, ref bool firstAvailable)
    {
        using var bar = Im.TabBar.Begin("tabs"u8);
        if (!bar)
            return;

        var table          = new ColorTable.Table();
        var highLightColor = ColorId.AdvancedDyeActive.Value();
        for (byte i = 0; i < MaterialService.MaterialsPerModel; ++i)
        {
            var index = _drawIndex!.Value with { MaterialIndex = i };
            var available = index.TryGetTexture(textures, materials, out var texture, out _mode)
             && directX.TryGetColorTable(*texture, out table);


            if (index == preview.LastValueIndex with { RowIndex = 0 })
                table = preview.LastOriginalColorTable;

            using var disable = Im.Disabled(!available);
            var select = available && firstAvailable && _selectedMaterial == byte.MaxValue
                ? TabItemFlags.SetSelected
                : TabItemFlags.None;

            if (available)
                firstAvailable = false;

            var       hasAdvancedDyes = _state.Materials.CheckExistenceMaterial(index);
            using var c               = ImGuiColor.Text.Push(highLightColor, hasAdvancedDyes);
            using var tab             = _label.TabItem(i, select);
            c.Pop();
            if (Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
            {
                using var enabled = Im.Enabled();
                var (path, gamePath) = ResourceName(index);
                using var tt = Im.Tooltip.Begin();

                if (gamePath.Length is 0 || path.Length is 0)
                    Im.Text("This material does not exist."u8);
                else if (!available)
                    Im.Text($"This material does not have an associated color set.\n\n{gamePath}\n{path}");
                else
                    Im.Text($"{gamePath}\n{path}");

                if (hasAdvancedDyes && !available)
                {
                    Im.Text("\nRight-Click to remove ineffective advanced dyes."u8);
                    if (Im.Mouse.IsClicked(MouseButton.Right))
                        for (byte row = 0; row < ColorTable.NumRows; ++row)
                            stateManager.ResetMaterialValue(_state, index with { RowIndex = row }, ApplySettings.Game);
                }
            }

            if ((tab.Success || select is TabItemFlags.SetSelected) && available)
            {
                _selectedMaterial = i;
                DrawToggles();
                DrawTable(index, table);
            }
        }
    }

    private void DrawToggles()
    {
        var layerButtonWidth = _mode is ColorRow.Mode.Dawntrail ? Im.Font.Mono.GetCharacterAdvance(' ') * 5 + Im.Style.FramePadding.X * 2 : 0;
        var buttonWidth =
            (Im.ContentRegion.Available.X - (_mode is ColorRow.Mode.Dawntrail ? layerButtonWidth * 2 + Im.Style.ItemSpacing.X : 0)) / 2;
        var       buttonSize = new Vector2(buttonWidth, 0);
        using var font       = Im.Font.PushMono();
        using var hoverColor = ImGuiColor.ButtonHovered.Push(Im.Style[ImGuiColor.TabHovered]);

        hoverColor.Push(ImGuiColor.Button, Im.Style[_rowOffset is 0 ? ImGuiColor.TabSelected : ImGuiColor.Tab]);
        if (ImEx.ButtonCorners("Row Pairs 1-8 "u8, buttonSize, ButtonFlags.MouseButtonLeft, Corners.Left))
            _rowOffset = 0;
        hoverColor.Pop();

        Im.Line.NoSpacing();

        hoverColor.Push(ImGuiColor.Button, Im.Style[_rowOffset is RowsPerPage ? ImGuiColor.TabSelected : ImGuiColor.Tab]);
        if (ImEx.ButtonCorners("Row Pairs 9-16"u8, buttonSize, ButtonFlags.MouseButtonLeft, Corners.Right))
            _rowOffset = RowsPerPage;
        hoverColor.Pop();

        if (_mode is ColorRow.Mode.Dawntrail)
        {
            Im.Line.Same();

            buttonSize = new Vector2(layerButtonWidth, 0);

            hoverColor.Push(ImGuiColor.Button, Im.Style[!_editSheen ? ImGuiColor.TabSelected : ImGuiColor.Tab]);
            if (ImEx.ButtonCorners("Base"u8, buttonSize, ButtonFlags.MouseButtonLeft, Corners.Left))
                _editSheen = false;
            hoverColor.Pop();

            Im.Line.NoSpacing();

            hoverColor.Push(ImGuiColor.Button, Im.Style[_editSheen ? ImGuiColor.TabSelected : ImGuiColor.Tab]);
            if (ImEx.ButtonCorners("Sheen"u8, buttonSize, ButtonFlags.MouseButtonLeft, Corners.Right))
                _editSheen = true;
            hoverColor.Pop();
        }
    }

    private void DrawContent(ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<Texture>> textures,
        ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<Material>> materials)
    {
        var firstAvailable = true;
        DrawTabBar(textures, materials, ref firstAvailable);

        if (firstAvailable)
            Im.Text("No Editable Materials available."u8);
    }

    private void DrawWindow(ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<Texture>> textures,
        ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<Material>> materials)
    {
        var flags = WindowFlags.NoFocusOnAppearing
          | WindowFlags.NoCollapse
          | WindowFlags.NoDecoration
          | WindowFlags.NoResize
          | WindowFlags.NoDocking;

        // Set position to the right of the main window when attached
        // The downwards offset is implicit through child position.
        if (config.KeepAdvancedDyesAttached)
        {
            var position = Im.Window.Position;
            position.X += Im.Window.Size.X + Im.Style.WindowPadding.X;
            Im.Window.SetNextPosition(position);
            flags |= WindowFlags.NoMove;
        }

        var width = 7 * Im.Style.FrameHeight // Buttons
          + 3 * Im.Style.ItemSpacing.X       // around text
          + 7 * Im.Style.ItemInnerSpacing.X
          + 200 * Im.Style.GlobalScale                                       // Drags
          + 7 * Im.Font.Mono.GetCharacterAdvance(' ') * Im.Style.GlobalScale // Row
          + 2 * Im.Style.WindowPadding.X;
        var height = 19 * Im.Style.FrameHeightWithSpacing + Im.Style.WindowPadding.Y + 3 * Im.Style.ItemSpacing.Y;
        Im.Window.SetNextSize(new Vector2(width, height));

        using var window = Im.Window.Begin("###Glamourer Advanced Dyes"u8, flags);
        if (Im.Window.Appearing || _forceFocus)
        {
            Im.Window.SetFocus("###Glamourer Advanced Dyes"u8);
            _forceFocus = false;
        }

        if (window)
            DrawContent(textures, materials);
    }

    public void Draw(Actor actor, ActorState state)
    {
        _actor = actor;
        _state = state;
        if (!ShouldBeDrawn())
            return;

        if (_drawIndex!.Value.TryGetTextures(actor, out var textures, out var materials))
            DrawWindow(textures, materials);
    }

    private void DrawTable(MaterialValueIndex materialIndex, ColorTable.Table table)
    {
        if (!materialIndex.Valid)
            return;

        using var disabled = Im.Disabled(_state.IsLocked);
        _anyChanged = false;
        for (byte i = 0; i < RowsPerPage; ++i)
        {
            var     actualI = (byte)(i + _rowOffset);
            var     index   = materialIndex with { RowIndex = actualI };
            ref var row     = ref table[actualI];
            DrawRow(ref row, index, table);
        }

        Im.Separator();
        DrawAllRow(materialIndex, table);
    }

    private static void CopyToClipboard(in ColorTable.Table table)
    {
        try
        {
            fixed (ColorTable.Table* ptr = &table)
            {
                var data   = new ReadOnlySpan<byte>(ptr, sizeof(ColorTable.Table));
                var base64 = Convert.ToBase64String(data);
                Im.Clipboard.Set(base64);
            }
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not copy color table to clipboard:\n{ex}");
        }
    }

    private static bool ImportFromClipboard(out ColorTable.Table table)
    {
        try
        {
            var base64 = Im.Clipboard.GetUtf16();
            if (base64.Length > 0)
            {
                var data = Convert.FromBase64String(base64);
                if (sizeof(ColorTable.Table) <= data.Length)
                {
                    table = new ColorTable.Table();
                    fixed (ColorTable.Table* tPtr = &table)
                    {
                        fixed (byte* ptr = data)
                        {
                            new ReadOnlySpan<byte>(ptr, sizeof(ColorTable.Table)).CopyTo(new Span<byte>(tPtr, sizeof(ColorTable.Table)));
                            return true;
                        }
                    }
                }
            }

            if (ColorRowClipboard.IsTableSet)
            {
                table = ColorRowClipboard.Table;
                return true;
            }
        }
        catch (Exception ex)
        {
            Glamourer.Messager.AddMessage(new Notification(ex, "Could not paste color table from clipboard.",
                "Could not paste color table from clipboard."));
        }

        table = default;
        return false;
    }

    private void DrawAllRow(MaterialValueIndex materialIndex, in ColorTable.Table table)
    {
        using var id = Im.Id.Push(100);
        ImEx.Icon.Button(LunaStyle.OnHoverIcon, "Highlight all affected colors on the character."u8);
        if (Im.Item.Hovered())
            preview.OnHover(materialIndex with { RowIndex = byte.MaxValue }, _actor.Index, table);
        Im.Line.Same();
        using (Im.Font.PushMono())
        {
            ImEx.TextFrameAligned("All Color Row Pairs (1-16)"u8);
        }

        var spacing = Im.Style.ItemInnerSpacing.X;
        Im.Line.Same(Im.Window.Size.X - 3 * Im.Style.FrameHeight - 2 * spacing - Im.Style.WindowPadding.X);
        if (ImEx.Icon.Button(LunaStyle.ToClipboardIcon, "Export this table to your clipboard."u8))
        {
            ColorRowClipboard.Table     = table;
            ColorRowClipboard.TableMode = _mode;
            CopyToClipboard(table);
        }

        Im.Line.SameInner();
        if (ImEx.Icon.Button(LunaStyle.FromClipboardIcon, "Import an exported table from your clipboard onto this table."u8)
         && ImportFromClipboard(out var newTable))
            for (var idx = 0; idx < ColorTable.NumRows; ++idx)
            {
                var row         = newTable[idx];
                var internalRow = ColorRow.From(row, _mode);
                var slot        = materialIndex.ToEquipSlot();
                var weapon = slot is EquipSlot.MainHand or EquipSlot.OffHand
                    ? _state.ModelData.Weapon(slot)
                    : _state.ModelData.Armor(slot).ToWeapon(0);
                var value = new MaterialValueState(internalRow, internalRow, weapon, StateSource.Manual);
                stateManager.ChangeMaterialValue(_state, materialIndex with { RowIndex = (byte)idx }, value, ApplySettings.Manual);
            }

        Im.Line.SameInner();
        if (ImEx.Icon.Button(LunaStyle.UndoIcon, "Reset this table to game state."u8, !_anyChanged))
            for (byte i = 0; i < ColorTable.NumRows; ++i)
                stateManager.ResetMaterialValue(_state, materialIndex with { RowIndex = i }, ApplySettings.Game);
    }

    private void DrawRow(ref ColorTableRow row, MaterialValueIndex index, in ColorTable.Table table)
    {
        using var id      = Im.Id.Push((uint)index.RowIndex);
        var       changed = _state.Materials.TryGetValue(index, out var value);
        if (!changed)
        {
            var internalRow = ColorRow.From(row, _mode);
            var slot        = index.ToEquipSlot();
            var weapon = slot switch
            {
                EquipSlot.MainHand => _state.ModelData.Weapon(EquipSlot.MainHand),
                EquipSlot.OffHand  => _state.ModelData.Weapon(EquipSlot.OffHand),
                EquipSlot.Unknown =>
                    _state.ModelData.BonusItem((index.SlotIndex - 16u).ToBonusSlot()).Armor().ToWeapon(0), // TODO: Handle better
                _ => _state.ModelData.Armor(slot).ToWeapon(0),
            };
            value = new MaterialValueState(internalRow, internalRow, weapon, StateSource.Manual);
        }
        else
        {
            _anyChanged = true;
            value = new MaterialValueState(value.Game,
                value.Model.IsPartial(_mode) ? value.Model.MergeOnto(ColorRow.From(row, _mode)) : value.Model,
                value.DrawData, StateSource.Manual);
        }

        ImEx.Icon.Button(LunaStyle.OnHoverIcon, "Highlight the affected colors on the character."u8);
        if (Im.Item.Hovered())
            preview.OnHover(index, _actor.Index, table);

        Im.Line.Same();
        using (Im.Font.PushMono())
        {
            var rowIndex  = index.RowIndex / 2 + 1;
            var rowSuffix = (index.RowIndex & 1) == 0 ? 'A' : 'B';
            ImEx.TextFrameAligned($"Row {rowIndex,2}{rowSuffix}");
        }

        Im.Line.Same(0, Im.Style.ItemSpacing.X * 2);
        var applied = ImEx.ColorPickerButton("##diffuse"u8, "Change the diffuse color for this row."u8, value.Model.Diffuse,
            out value.Model.Diffuse, 'D');

        var spacing = Im.Style.ItemInnerSpacing;
        Im.Line.Same(0, spacing.X);
        applied |= ImEx.ColorPickerButton("##specular"u8, "Change the specular color for this row."u8, value.Model.Specular,
            out value.Model.Specular, 'S');

        Im.Line.Same(0, spacing.X);
        applied |= ImEx.ColorPickerButton("##emissive"u8, "Change the emissive color for this row."u8, value.Model.Emissive,
            out value.Model.Emissive, 'E');
        
        Im.Line.Same(0, spacing.X);

        if (_mode is ColorRow.Mode.Dawntrail && _editSheen)
        {
            // The other layout has 2 items of width 100*sc and one spacing, for a total of 200*sc + 1*sp.
            // This layout has 3 items and two spacings: 3*w + 2*sp = 200*sc + 1*sp.
            var allItemsWidth = 200 * Im.Style.GlobalScale - spacing.X;
            var itemWidth     = MathF.Floor(allItemsWidth / 3);

            Im.Item.SetNextWidth(allItemsWidth - itemWidth * 2);
            applied |= DragSheen(ref value.Model.Sheen, false);
            Im.Tooltip.OnHover("Change the sheen strength for this row."u8);

            Im.Line.Same(0, spacing.X);

            Im.Item.SetNextWidth(itemWidth);
            applied |= DragSheenTint(ref value.Model.SheenTint, false);
            Im.Tooltip.OnHover("Change the sheen tint for this row."u8);

            Im.Line.Same(0, spacing.X);

            Im.Item.SetNextWidth(itemWidth);
            applied |= DragSheenRoughness(ref value.Model.SheenAperture, false);
            Im.Tooltip.OnHover("Change the sheen roughness for this row."u8);
        }
        else
        {
            Im.Item.SetNextWidthScaled(100);
            var editAsRoughness = config.RoughnessSetting.Get(_mode is ColorRow.Mode.Dawntrail);
            applied |= (_mode, editAsRoughness) switch
            {
                (ColorRow.Mode.Legacy, false)    => DragGloss(ref value.Model.GlossStrength, false),
                (ColorRow.Mode.Legacy, true)     => DragGlossAsRoughness(ref value.Model.GlossStrength, false),
                (ColorRow.Mode.Dawntrail, false) => DragRoughnessAsGloss(ref value.Model.Roughness, false),
                (ColorRow.Mode.Dawntrail, true)  => DragRoughness(ref value.Model.Roughness, false),
                _                                => false,
            };
            Im.Tooltip.OnHover(editAsRoughness ? "Change the roughness for this row."u8 : "Change the gloss strength for this row."u8);

            Im.Line.Same(0, spacing.X);
            if (_mode is not ColorRow.Mode.Dawntrail)
            {
                Im.Item.SetNextWidthScaled(100);
                applied |= DragSpecularStrength(ref value.Model.SpecularStrength, false);
                Im.Tooltip.OnHover("Change the specular strength for this row."u8);
            }
            else
            {
                Im.Item.SetNextWidthScaled(100);
                applied |= DragMetalness(ref value.Model.Metalness, false);
                Im.Tooltip.OnHover("Change the metalness for this row."u8);
            }
        }

        Im.Line.Same(0, spacing.X);
        if (ImEx.Icon.Button(LunaStyle.ToClipboardIcon, "Export this row to your clipboard."u8))
        {
            ColorRowClipboard.Row     = value.Model;
            ColorRowClipboard.RowMode = _mode;
        }

        Im.Line.Same(0, spacing.X);
        if (ImEx.Icon.Button(LunaStyle.FromClipboardIcon, "Import an exported row from your clipboard onto this row."u8,
                !ColorRowClipboard.IsSet))
        {
            value.Model = ColorRowClipboard.Row;
            applied     = true;
        }

        Im.Line.Same(0, spacing.X);
        if (ImEx.Icon.Button(LunaStyle.UndoIcon, "Reset this row to game state."u8, !changed))
            stateManager.ResetMaterialValue(_state, index, ApplySettings.Game);

        if (applied)
            stateManager.ChangeMaterialValue(_state, index, value, ApplySettings.Manual);
    }

    public static bool DragGloss(ref float value, bool canUnset)
    {
        var tmp      = float.IsNaN(value) ? ColorRow.DefaultGlossStrength : value;
        var minValue = Im.Io.KeyControl ? 0f : (float)Half.Epsilon;
        if (!Im.Drag("##Gloss"u8, ref tmp, float.IsNaN(value) ? "\u2014 G"u8 : "%.1f G"u8, 0.001f, minValue, Math.Max(0.01f, 0.005f * value), SliderFlags.AlwaysClamp))
            return UnsetBehavior(ref value, canUnset);

        var tmp2 = Math.Clamp(tmp, minValue, (float)Half.MaxValue);
        if (tmp2 == value)
            return false;

        value = tmp2;
        return true;
    }

    public static bool DragGlossAsRoughness(ref float value, bool canUnset)
    {
        var roughness = ColorTableRow.RoughnessFromShininess(float.IsNaN(value) ? ColorRow.DefaultGlossStrength : value);
        var tmp       = roughness * 100f;
        if (!Im.Drag("##Gloss"u8, ref tmp, float.IsNaN(value) ? "\u2014 Rg"u8 : "%.0f%% Rg"u8, 0f, 100f, 0.25f, SliderFlags.AlwaysClamp))
            return UnsetBehavior(ref value, canUnset);

        var tmp2 = Math.Clamp(tmp, 0f, 100f) / 100f;
        if (tmp2 == roughness)
            return false;

        value = ColorTableRow.ShininessFromRoughness(tmp2);
        return true;
    }

    public static bool DragSpecularStrength(ref float value, bool canUnset)
    {
        var tmp = (float.IsNaN(value) ? ColorRow.DefaultSpecularStrength : value) * 100f;
        if (!Im.Drag("##SpecularStrength"u8, ref tmp, float.IsNaN(value) ? "\u2014 SS"u8 : "%.0f%% SS"u8, 0f, (float)Half.MaxValue * 100f, 0.05f, SliderFlags.AlwaysClamp))
            return UnsetBehavior(ref value, canUnset);

        var tmp2 = Math.Clamp(tmp, 0f, (float)Half.MaxValue * 100f) / 100f;
        if (tmp2 == value)
            return false;

        value = tmp2;
        return true;
    }

    public static bool DragRoughness(ref float value, bool canUnset)
    {
        var tmp = (float.IsNaN(value) ? ColorRow.DefaultRoughness : value) * 100f;
        if (!Im.Drag("##Roughness"u8, ref tmp, float.IsNaN(value) ? "\u2014 Rg"u8 : "%.0f%% Rg"u8, 0f, 100f, 0.25f, SliderFlags.AlwaysClamp))
            return UnsetBehavior(ref value, canUnset);

        var tmp2 = Math.Clamp(tmp, 0f, 100f) / 100f;
        if (tmp2 == value)
            return false;

        value = tmp2;
        return true;
    }

    public static bool DragRoughnessAsGloss(ref float value, bool canUnset)
    {
        var gloss = ColorTableRow.ShininessFromRoughness(float.IsNaN(value) ? ColorRow.DefaultRoughness : value);
        var tmp   = gloss;
        if (!Im.Drag("##Roughness"u8, ref tmp, float.IsNaN(value) ? "\u2014 G"u8 : "%.1f G"u8, 0.001f, (float)Half.Epsilon, Math.Max(0.01f, 0.005f * gloss), SliderFlags.AlwaysClamp))
            return UnsetBehavior(ref value, canUnset);

        var tmp2 = Math.Clamp(tmp, (float)Half.Epsilon, (float)Half.MaxValue);
        if (tmp2 == gloss)
            return false;

        value = ColorTableRow.RoughnessFromShininess(tmp2);
        return true;
    }

    public static bool DragMetalness(ref float value, bool canUnset)
    {
        var tmp = (float.IsNaN(value) ? ColorRow.DefaultMetalness : value) * 100f;
        if (!Im.Drag("##Metalness"u8, ref tmp, float.IsNaN(value) ? "\u2014 Mt"u8 : "%.0f%% Mt"u8, 0f, 100f, 0.25f, SliderFlags.AlwaysClamp))
            return UnsetBehavior(ref value, canUnset);

        var tmp2 = Math.Clamp(tmp, 0f, 100f) / 100f;
        if (tmp2 == value)
            return false;

        value = tmp2;
        return true;
    }

    public static bool DragSheen(ref float value, bool canUnset)
    {
        var tmp = (float.IsNaN(value) ? ColorRow.DefaultSheen : value) * 100f;
        if (!Im.Drag("##Sheen"u8, ref tmp, float.IsNaN(value) ? "\u2014 Sh"u8 : "%.0f%% Sh"u8, 0f, 100f * (float)Half.MaxValue, 0.25f, SliderFlags.AlwaysClamp))
            return UnsetBehavior(ref value, canUnset);

        var tmp2 = Math.Clamp(tmp, 0f, 100f * (float)Half.MaxValue) / 100f;
        if (tmp2 == value)
            return false;

        value = tmp2;
        return true;
    }

    public static bool DragSheenTint(ref float value, bool canUnset)
    {
        var tmp = (float.IsNaN(value) ? ColorRow.DefaultSheenTint : value) * 100f;
        if (!Im.Drag("##SheenTint"u8, ref tmp, float.IsNaN(value) ? "\u2014 ST"u8 : "%.0f%% ST"u8, -100f * (float)Half.MaxValue, 100f * (float)Half.MaxValue, 0.25f,
                SliderFlags.AlwaysClamp))
            return UnsetBehavior(ref value, canUnset);

        var tmp2 = Math.Clamp(tmp, -100f * (float)Half.MaxValue, 100f * (float)Half.MaxValue) / 100f;
        if (tmp2 == value)
            return false;

        value = tmp2;
        return true;
    }

    public static bool DragSheenRoughness(ref float value, bool canUnset)
    {
        var tmp = 100f / (float.IsNaN(value) ? ColorRow.DefaultSheenAperture : value);
        if (!Im.Drag("##SheenAperture"u8, ref tmp, float.IsNaN(value) ? "\u2014 SR"u8 : "%.0f%% SR"u8, 100f / (float)Half.MaxValue, 100f / (float)Half.Epsilon, 0.25f,
                SliderFlags.AlwaysClamp))
            return UnsetBehavior(ref value, canUnset);

        var tmp2 = Math.Clamp(100f / tmp, (float)Half.Epsilon, (float)Half.MaxValue);
        if (tmp2 == value)
            return false;

        value = tmp2;
        return true;
    }

    private static bool UnsetBehavior(ref float value, bool canUnset)
    {
        if (!(canUnset && Im.Item.RightClicked() && Im.Io.KeyControl))
            return false;

        value = float.NaN;
        return true;
    }

    private LabelStruct _label = new();

    private struct LabelStruct
    {
        private fixed byte _label[5];

        public Im.TabItemDisposable TabItem(byte materialIndex, TabItemFlags flags)
        {
            _label[4] = (byte)('A' + materialIndex);
            return Im.TabBar.BeginItem(MemoryMarshal.CreateReadOnlySpan(ref _label[0], 5), flags | TabItemFlags.NoTooltip);
        }

        public LabelStruct()
        {
            _label[0] = (byte)'M';
            _label[1] = (byte)'a';
            _label[2] = (byte)'t';
            _label[3] = (byte)' ';
            _label[5] = 0;
        }
    }
}
