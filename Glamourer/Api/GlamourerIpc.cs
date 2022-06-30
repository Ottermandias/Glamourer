using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;

namespace Glamourer.Api;

public class GlamourerIpc : IDisposable
{
    public const int    CurrentApiVersion                              = 0;
    public const string LabelProviderApiVersion                        = "Glamourer.ApiVersion";
    public const string LabelProviderGetAllCustomization               = "Glamourer.GetAllCustomization";
    public const string LabelProviderGetAllCustomizationFromCharacter  = "Glamourer.GetAllCustomizationFromCharacter";
    public const string LabelProviderApplyAll                          = "Glamourer.ApplyAll";
    public const string LabelProviderApplyAllToCharacter               = "Glamourer.ApplyAllToCharacter";
    public const string LabelProviderApplyOnlyEquipment                = "Glamourer.ApplyOnlyEquipment";
    public const string LabelProviderApplyOnlyEquipmentToCharacter     = "Glamourer.ApplyOnlyEquipmentToCharacter";
    public const string LabelProviderApplyOnlyCustomization            = "Glamourer.ApplyOnlyCustomization";
    public const string LabelProviderApplyOnlyCustomizationToCharacter = "Glamourer.ApplyOnlyCustomizationToCharacter";
    public const string LabelProviderRevert                            = "Glamourer.Revert";
    public const string LabelProviderRevertCharacter                   = "Glamourer.RevertCharacter";

    private readonly ClientState            _clientState;
    private readonly ObjectTable            _objectTable;
    private readonly DalamudPluginInterface _pluginInterface;

    internal ICallGateProvider<string, string?>?            ProviderGetAllCustomization;
    internal ICallGateProvider<Character?, string?>?        ProviderGetAllCustomizationFromCharacter;
    internal ICallGateProvider<string, string, object>?     ProviderApplyAll;
    internal ICallGateProvider<string, Character?, object>? ProviderApplyAllToCharacter;
    internal ICallGateProvider<string, string, object>?     ProviderApplyOnlyCustomization;
    internal ICallGateProvider<string, Character?, object>? ProviderApplyOnlyCustomizationToCharacter;
    internal ICallGateProvider<string, string, object>?     ProviderApplyOnlyEquipment;
    internal ICallGateProvider<string, Character?, object>? ProviderApplyOnlyEquipmentToCharacter;
    internal ICallGateProvider<string, object>?             ProviderRevert;
    internal ICallGateProvider<Character?, object>?         ProviderRevertCharacter;
    internal ICallGateProvider<int>?                        ProviderGetApiVersion;

    public GlamourerIpc(ClientState clientState, ObjectTable objectTable, DalamudPluginInterface pluginInterface)
    {
        _clientState = clientState;
        _objectTable = objectTable;
        _pluginInterface = pluginInterface;

        InitializeProviders();
    }

    public void Dispose()
        => DisposeProviders();

    private void DisposeProviders()
    {
        ProviderGetAllCustomization?.UnregisterFunc();
        ProviderGetAllCustomizationFromCharacter?.UnregisterFunc();
        ProviderApplyAll?.UnregisterAction();
        ProviderApplyAllToCharacter?.UnregisterAction();
        ProviderApplyOnlyCustomization?.UnregisterAction();
        ProviderApplyOnlyCustomizationToCharacter?.UnregisterAction();
        ProviderApplyOnlyEquipment?.UnregisterAction();
        ProviderApplyOnlyEquipmentToCharacter?.UnregisterAction();
        ProviderRevert?.UnregisterAction();
        ProviderRevertCharacter?.UnregisterAction();
        ProviderGetApiVersion?.UnregisterFunc();

    }

    private void InitializeProviders()
    {
        try
        {
            ProviderGetApiVersion = _pluginInterface.GetIpcProvider<int>(LabelProviderApiVersion);
            ProviderGetApiVersion.RegisterFunc(GetApiVersion);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApiVersion}.");
        }

        try
        {
            ProviderGetAllCustomization = _pluginInterface.GetIpcProvider<string, string?>(LabelProviderGetAllCustomization);
            ProviderGetAllCustomization.RegisterFunc(GetAllCustomization);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyOnlyEquipment}.");
        }

        try
        {
            ProviderGetAllCustomizationFromCharacter = _pluginInterface.GetIpcProvider<Character?, string?>(LabelProviderGetAllCustomizationFromCharacter);
            ProviderGetAllCustomizationFromCharacter.RegisterFunc(GetAllCustomization);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderGetAllCustomizationFromCharacter}.");
        }

        try
        {
            ProviderApplyAll =
                _pluginInterface.GetIpcProvider<string, string, object>(LabelProviderApplyAll);
            ProviderApplyAll.RegisterAction(ApplyAll);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyAll}.");
        }

        try
        {
            ProviderApplyAllToCharacter =
                _pluginInterface.GetIpcProvider<string, Character?, object>(LabelProviderApplyAllToCharacter);
            ProviderApplyAllToCharacter.RegisterAction(ApplyAll);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyAll}.");
        }

        try
        {
            ProviderApplyOnlyCustomization =
                _pluginInterface.GetIpcProvider<string, string, object>(LabelProviderApplyOnlyCustomization);
            ProviderApplyOnlyCustomization.RegisterAction(ApplyOnlyCustomization);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyOnlyCustomization}.");
        }

        try
        {
            ProviderApplyOnlyCustomizationToCharacter =
                _pluginInterface.GetIpcProvider<string, Character?, object>(LabelProviderApplyOnlyCustomizationToCharacter);
            ProviderApplyOnlyCustomizationToCharacter.RegisterAction(ApplyOnlyCustomization);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyOnlyCustomization}.");
        }

        try
        {
            ProviderApplyOnlyEquipment =
                _pluginInterface.GetIpcProvider<string, string, object>(LabelProviderApplyOnlyEquipment);
            ProviderApplyOnlyEquipment.RegisterAction(ApplyOnlyEquipment);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyOnlyEquipment}.");
        }

        try
        {
            ProviderApplyOnlyEquipmentToCharacter =
                _pluginInterface.GetIpcProvider<string, Character?, object>(LabelProviderApplyOnlyEquipmentToCharacter);
            ProviderApplyOnlyEquipmentToCharacter.RegisterAction(ApplyOnlyEquipment);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyOnlyEquipment}.");
        }

        try
        {
            ProviderRevert =
                _pluginInterface.GetIpcProvider<string, object>(LabelProviderRevert);
            ProviderRevert.RegisterAction(Revert);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderRevert}.");
        }

        try
        {
            ProviderRevertCharacter =
                _pluginInterface.GetIpcProvider<Character?, object>(LabelProviderRevertCharacter);
            ProviderRevertCharacter.RegisterAction(Revert);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderRevert}.");
        }
    }

    private static int GetApiVersion()
        => CurrentApiVersion;

    private void ApplyAll(string customization, string characterName)
    {
        var save = CharacterSave.FromString(customization);
        foreach (var gameObject in _objectTable)
        {
            if (gameObject.Name.ToString() != characterName)
                continue;

            var player = (Character)gameObject;
            Glamourer.RevertableDesigns.Revert(player);
            save.Apply(player);
            Glamourer.Penumbra.UpdateCharacters(player, null);
            break;
        }
    }

    private void ApplyAll(string customization, Character? character)
    {
        if (character == null)
            return;
        var save = CharacterSave.FromString(customization);
        Glamourer.RevertableDesigns.Revert(character);
        save.Apply(character);
        Glamourer.Penumbra.UpdateCharacters(character, null);
    }

    private void ApplyOnlyCustomization(string customization, string characterName)
    {
        var save = CharacterSave.FromString(customization);
        foreach (var gameObject in _objectTable)
        {
            if (gameObject.Name.ToString() != characterName)
                continue;

            var player = (Character)gameObject;
            Glamourer.RevertableDesigns.Revert(player);
            save.ApplyOnlyCustomizations(player);
            Glamourer.Penumbra.UpdateCharacters(player, null);
            break;
        }
    }

    private void ApplyOnlyCustomization(string customization, Character? character)
    {
        if (character == null)
            return;
        var save = CharacterSave.FromString(customization);
        Glamourer.RevertableDesigns.Revert(character);
        save.ApplyOnlyCustomizations(character);
        Glamourer.Penumbra.UpdateCharacters(character, null);
    }

    private void ApplyOnlyEquipment(string customization, string characterName)
    {
        var save = CharacterSave.FromString(customization);
        foreach (var gameObject in _objectTable)
        {
            if (gameObject.Name.ToString() != characterName)
                continue;

            var player = (Character)gameObject;
            Glamourer.RevertableDesigns.Revert(player);
            save.ApplyOnlyEquipment(player);
            Glamourer.Penumbra.UpdateCharacters(player, null);
            break;
        }
    }

    private void ApplyOnlyEquipment(string customization, Character? character)
    {
        if (character == null)
            return;
        var save = CharacterSave.FromString(customization);
        Glamourer.RevertableDesigns.Revert(character);
        save.ApplyOnlyEquipment(character);
        Glamourer.Penumbra.UpdateCharacters(character, null);
    }

    private void Revert(string characterName)
    {
        foreach (var gameObject in _objectTable)
        {
            if (gameObject.Name.ToString() != characterName)
                continue;

            var player = (Character)gameObject;
            Glamourer.RevertableDesigns.Revert(player);
            Glamourer.Penumbra.UpdateCharacters(player, null);
            return;
        }

        Glamourer.RevertableDesigns.RevertByNameWithoutApplication(characterName);
    }

    private void Revert(Character? character)
    {
        if (character == null)
            return;
        Glamourer.RevertableDesigns.Revert(character);
        Glamourer.Penumbra.UpdateCharacters(character, null);
    }

    private string? GetAllCustomization(Character? character)
    {
        if (character == null)
            return null;

        CharacterSave save = new CharacterSave();
        save.LoadCharacter(character);
        return save.ToBase64();
    }

    private string? GetAllCustomization(string characterName)
    {
        CharacterSave save = null!;
        foreach (var gameObject in _objectTable)
        {
            if (gameObject.Name.ToString() != characterName)
                continue;

            var player = (Character)gameObject;
            save = new CharacterSave();
            save.LoadCharacter(player);
            break;
        }

        return save?.ToBase64() ?? null;
    }
}
