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
    public const int    CurrentApiVersion                   = 0;
    public const string LabelProviderApiVersion             = "Glamourer.ApiVersion";
    public const string LabelProviderGetAllCustomization    = "Glamourer.GetAllCustomization";
    public const string LabelProviderApplyAll               = "Glamourer.ApplyAll";
    public const string LabelProviderApplyOnlyEquipment     = "Glamourer.ApplyOnlyEquipment";
    public const string LabelProviderApplyOnlyCustomization = "Glamourer.ApplyOnlyCustomization";
    public const string LabelProviderRevert                 = "Glamourer.Revert";

    private readonly ClientState            _clientState;
    private readonly ObjectTable            _objectTable;
    private readonly DalamudPluginInterface _pluginInterface;

    internal ICallGateProvider<string, string?>?        ProviderGetAllCustomization;
    internal ICallGateProvider<string, string, object>? ProviderApplyAll;
    internal ICallGateProvider<string, string, object>? ProviderApplyOnlyCustomization;
    internal ICallGateProvider<string, string, object>? ProviderApplyOnlyEquipment;
    internal ICallGateProvider<string, object>?         ProviderRevert;
    internal ICallGateProvider<int>?                    ProviderGetApiVersion;

    public GlamourerIpc(ClientState clientState, ObjectTable objectTable, DalamudPluginInterface pluginInterface)
    {
        _clientState     = clientState;
        _objectTable     = objectTable;
        _pluginInterface = pluginInterface;

        InitializeProviders();
    }

    public void Dispose()
        => DisposeProviders();

    private void DisposeProviders()
    {
        ProviderGetAllCustomization?.UnregisterFunc();
        ProviderApplyAll?.UnregisterAction();
        ProviderApplyOnlyCustomization?.UnregisterAction();
        ProviderApplyOnlyEquipment?.UnregisterAction();
        ProviderRevert?.UnregisterAction();
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
            ProviderRevert =
                _pluginInterface.GetIpcProvider<string, object>(LabelProviderRevert);
            ProviderRevert.RegisterAction(Revert);
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
