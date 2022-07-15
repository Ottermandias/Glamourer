
namespace Glamourer.Gui;

internal partial class Interface
{
    //private readonly CharacterSave _currentSave   = new();
    //private          string        _newDesignName = string.Empty;
    //private          bool          _keyboardFocus;
    //private          bool          _holdShift;
    //private          bool          _holdCtrl;
    //private const    string        DesignNamePopupLabel = "Save Design As...";
    //private const    uint          RedHeaderColor       = 0xFF1818C0;
    //private const    uint          GreenHeaderColor     = 0xFF18C018;
    //
    //private void DrawPlayerHeader()
    //{
    //    var color       = _player == null ? RedHeaderColor : GreenHeaderColor;
    //    var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
    //    using var c = ImRaii.PushColor(ImGuiCol.Text, color)
    //        .Push(ImGuiCol.Button,        buttonColor)
    //        .Push(ImGuiCol.ButtonHovered, buttonColor)
    //        .Push(ImGuiCol.ButtonActive,  buttonColor);
    //    using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
    //        .Push(ImGuiStyleVar.FrameRounding, 0);
    //    ImGui.Button($"{_currentLabel}##playerHeader", -Vector2.UnitX * 0.0001f);
    //}
    //
    //private static void DrawCopyClipboardButton(CharacterSave save)
    //{
    //    ImGui.PushFont(UiBuilder.IconFont);
    //    if (ImGui.Button(FontAwesomeIcon.Clipboard.ToIconString()))
    //        ImGui.SetClipboardText(save.ToBase64());
    //    ImGui.PopFont();
    //    ImGuiUtil.HoverTooltip("Copy customization code to clipboard.");
    //}
    //
    //private static void ConditionalApply(CharacterSave save, Character player)
    //{
    //    if (ImGui.GetIO().KeyShift)
    //        save.ApplyOnlyCustomizations(player);
    //    else if (ImGui.GetIO().KeyCtrl)
    //        save.ApplyOnlyEquipment(player);
    //    else
    //        save.Apply(player);
    //}
    //
    //private static CharacterSave ConditionalCopy(CharacterSave save, bool shift, bool ctrl)
    //{
    //    var copy = save.Copy();
    //    if (shift)
    //    {
    //        copy.Load(new CharacterEquipment());
    //        copy.SetHatState    = false;
    //        copy.SetVisorState  = false;
    //        copy.SetWeaponState = false;
    //        copy.WriteEquipment = CharacterEquipMask.None;
    //    }
    //    else if (ctrl)
    //    {
    //        copy.Load(CharacterCustomization.Default);
    //        copy.SetHatState         = false;
    //        copy.SetVisorState       = false;
    //        copy.SetWeaponState      = false;
    //        copy.WriteCustomizations = false;
    //    }
    //
    //    return copy;
    //}
    //
    //private bool DrawApplyClipboardButton()
    //{
    //    ImGui.PushFont(UiBuilder.IconFont);
    //    var applyButton = ImGui.Button(FontAwesomeIcon.Paste.ToIconString()) && _player != null;
    //    ImGui.PopFont();
    //    ImGuiUtil.HoverTooltip(
    //        "Apply customization code from clipboard.\nHold Shift to apply only customizations.\nHold Control to apply only equipment.");
    //
    //    if (!applyButton)
    //        return false;
    //
    //    try
    //    {
    //        var text = ImGui.GetClipboardText();
    //        if (!text.Any())
    //            return false;
    //
    //        var save = CharacterSave.FromString(text);
    //        ConditionalApply(save, _player!);
    //    }
    //    catch (Exception e)
    //    {
    //        PluginLog.Information($"{e}");
    //        return false;
    //    }
    //
    //    return true;
    //}
    //
    //private void DrawSaveDesignButton()
    //{
    //    ImGui.PushFont(UiBuilder.IconFont);
    //    if (ImGui.Button(FontAwesomeIcon.Save.ToIconString()))
    //        OpenDesignNamePopup(DesignNameUse.SaveCurrent);
    //
    //    ImGui.PopFont();
    //    ImGuiUtil.HoverTooltip("Save the current design.\nHold Shift to save only customizations.\nHold Control to save only equipment.");
    //
    //    DrawDesignNamePopup(DesignNameUse.SaveCurrent);
    //}
    //
    //private void DrawTargetPlayerButton()
    //{
    //    if (ImGui.Button("Target Player"))
    //        Dalamud.Targets.SetTarget(_player);
    //}
    //
    //private void DrawApplyToPlayerButton(CharacterSave save)
    //{
    //    if (!ImGui.Button("Apply to Self"))
    //        return;
    //
    //    var player = _inGPose
    //        ? (Character?)Dalamud.Objects[GPoseObjectId]
    //        : Dalamud.ClientState.LocalPlayer;
    //    var fallback = _inGPose ? Dalamud.ClientState.LocalPlayer : null;
    //    if (player == null)
    //        return;
    //
    //    ConditionalApply(save, player);
    //    if (_inGPose)
    //        ConditionalApply(save, fallback!);
    //    Glamourer.Penumbra.UpdateCharacters(player, fallback);
    //}
    //
    //
    //private static Character? TransformToCustomizable(Character? actor)
    //{
    //    if (actor == null)
    //        return null;
    //
    //    if (actor.ModelType() == 0)
    //        return actor;
    //
    //    actor.SetModelType(0);
    //    CharacterCustomization.Default.Write(actor.Address);
    //    return actor;
    //}
    //
    //private void DrawApplyToTargetButton(CharacterSave save)
    //{
    //    if (!ImGui.Button("Apply to Target"))
    //        return;
    //
    //    var player = TransformToCustomizable(CharacterFactory.Convert(Dalamud.Targets.Target));
    //    if (player == null)
    //        return;
    //
    //    var fallBackCharacter = _gPoseActors.TryGetValue(player.Name.ToString(), out var f) ? f : null;
    //    ConditionalApply(save, player);
    //    if (fallBackCharacter != null)
    //        ConditionalApply(save, fallBackCharacter!);
    //    Glamourer.Penumbra.UpdateCharacters(player, fallBackCharacter);
    //}
    //
    //private void DrawRevertButton()
    //{
    //    if (!ImGuiUtil.DrawDisabledButton("Revert", Vector2.Zero, string.Empty, _player == null))
    //        return;
    //
    //    Glamourer.RevertableDesigns.Revert(_player!);
    //    var fallBackCharacter = _gPoseActors.TryGetValue(_player!.Name.ToString(), out var f) ? f : null;
    //    if (fallBackCharacter != null)
    //        Glamourer.RevertableDesigns.Revert(fallBackCharacter);
    //    Glamourer.Penumbra.UpdateCharacters(_player, fallBackCharacter);
    //}
    //
    //private void SaveNewDesign(CharacterSave save)
    //{
    //    try
    //    {
    //        var (folder, name) = _designs.FileSystem.CreateAllFolders(_newDesignName);
    //        if (!name.Any())
    //            return;
    //
    //        var newDesign = new Design(folder, name) { Data = save };
    //        folder.AddChild(newDesign);
    //        _designs.Designs[newDesign.FullName()] = save;
    //        _designs.SaveToFile();
    //    }
    //    catch (Exception e)
    //    {
    //        PluginLog.Error($"Could not save new design {_newDesignName}:\n{e}");
    //    }
    //}
    //
    //private void DrawMonsterPanel()
    //{
    //    if (DrawApplyClipboardButton())
    //        Glamourer.Penumbra.UpdateCharacters(_player!);
    //
    //    ImGui.SameLine();
    //    if (ImGui.Button("Convert to Character"))
    //    {
    //        TransformToCustomizable(_player);
    //        _currentLabel = _currentLabel.Replace("(Monster)", "(NPC)");
    //        Glamourer.Penumbra.UpdateCharacters(_player!);
    //    }
    //
    //    if (!_inGPose)
    //    {
    //        ImGui.SameLine();
    //        DrawTargetPlayerButton();
    //    }
    //
    //    var       currentModel = _player!.ModelType();
    //    using var combo        = ImRaii.Combo("Model Id", currentModel.ToString());
    //    if (!combo)
    //        return;
    //
    //    foreach (var (id, _) in _models.Skip(1))
    //    {
    //        if (!ImGui.Selectable($"{id:D6}##models", id == currentModel) || id == currentModel)
    //            continue;
    //
    //        _player!.SetModelType((int)id);
    //        Glamourer.Penumbra.UpdateCharacters(_player!);
    //    }
    //}
    //
    //private void DrawPlayerPanel()
    //{
    //    DrawCopyClipboardButton(_currentSave);
    //    ImGui.SameLine();
    //    var changes = !_currentSave.WriteProtected && DrawApplyClipboardButton();
    //    ImGui.SameLine();
    //    DrawSaveDesignButton();
    //    ImGui.SameLine();
    //    DrawApplyToPlayerButton(_currentSave);
    //    if (!_inGPose)
    //    {
    //        ImGui.SameLine();
    //        DrawApplyToTargetButton(_currentSave);
    //        if (_player != null && !_currentSave.WriteProtected)
    //        {
    //            ImGui.SameLine();
    //            DrawTargetPlayerButton();
    //        }
    //    }
    //
    //    var data = _currentSave;
    //    if (!_currentSave.WriteProtected)
    //    {
    //        ImGui.SameLine();
    //        DrawRevertButton();
    //    }
    //    else
    //    {
    //        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.8f);
    //        data = data.Copy();
    //    }
    //
    //    if (DrawCustomization(ref data.Customizations) && _player != null)
    //    {
    //        Glamourer.RevertableDesigns.Add(_player);
    //        _currentSave.Customizations.Write(_player.Address);
    //        changes = true;
    //    }
    //
    //    changes |= DrawEquip(data.Equipment);
    //    changes |= DrawMiscellaneous(data, _player);
    //
    //    if (_player != null && changes)
    //        Glamourer.Penumbra.UpdateCharacters(_player);
    //    if (_currentSave.WriteProtected)
    //        ImGui.PopStyleVar();
    //}
    //
    //private void DrawActorPanel()
    //{
    //    using var group = ImRaii.Group();
    //    DrawPlayerHeader();
    //    using var child = ImRaii.Child("##playerData", -Vector2.One, true);
    //    if (!child)
    //        return;
    //
    //    if (_player == null || _player.ModelType() == 0)
    //        DrawPlayerPanel();
    //    else
    //        DrawMonsterPanel();
    //}
}
