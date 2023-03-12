using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;
using Glamourer.Designs;
using Glamourer.State;
using Penumbra.Api.Enums;

namespace Glamourer;

public partial class Glamourer
{
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

        private readonly ObjectTable            _objectTable;
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly Glamourer              _glamourer;

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

        public GlamourerIpc(Glamourer glamourer, ClientState clientState, ObjectTable objectTable,
            DalamudPluginInterface pluginInterface)
        {
            _glamourer       = glamourer;
            _objectTable     = objectTable;
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
                ProviderGetAllCustomizationFromCharacter =
                    _pluginInterface.GetIpcProvider<Character?, string?>(LabelProviderGetAllCustomizationFromCharacter);
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
            foreach (var gameObject in _objectTable)
            {
                if (gameObject.Name.ToString() == characterName)
                {
                    ApplyAll(customization, gameObject as Character);
                    return;
                }
            }
        }

        private void ApplyAll(string customization, Character? character)
        {
            if (character == null)
                return;

            var design = Design.CreateTemporaryFromBase64(customization, true, true);
            var active = _glamourer._stateManager.GetOrCreateSave(character.Address);
            _glamourer._stateManager.ApplyDesign(active, design, false);
        }

        private void ApplyOnlyCustomization(string customization, string characterName)
        {
            foreach (var gameObject in _objectTable)
            {
                if (gameObject.Name.ToString() == characterName)
                {
                    ApplyOnlyCustomization(customization, gameObject as Character);
                    return;
                }
            }
        }

        private void ApplyOnlyCustomization(string customization, Character? character)
        {
            if (character == null)
                return;
            var design = Design.CreateTemporaryFromBase64(customization, true, false);
            var active = _glamourer._stateManager.GetOrCreateSave(character.Address);
            _glamourer._stateManager.ApplyDesign(active, design, false);
        }

        private void ApplyOnlyEquipment(string customization, string characterName)
        {
            foreach (var gameObject in _objectTable)
            {
                if (gameObject.Name.ToString() == characterName)
                {
                    ApplyOnlyEquipment(customization, gameObject as Character);
                    return;
                }
            }
        }

        private void ApplyOnlyEquipment(string customization, Character? character)
        {
            if (character == null)
                return;
            var design = Design.CreateTemporaryFromBase64(customization, false, true);
            var active = _glamourer._stateManager.GetOrCreateSave(character.Address);
            _glamourer._stateManager.ApplyDesign(active, design, false);
        }

        private void Revert(string characterName)
        {
            foreach (var gameObject in _objectTable)
            {
                if (gameObject.Name.ToString() != characterName)
                    continue;

                Revert(gameObject as Character);
            }
        }

        private void Revert(Character? character)
        {
            if (character == null)
                return;

            var ident = Actors.FromObject(character, true, false, false);
            _glamourer._stateManager.DeleteSave(ident);
            _glamourer._penumbra.RedrawObject(character.Address, RedrawType.Redraw);
        }

        private string? GetAllCustomization(Character? character)
        {
            if (character == null)
                return null;

            var ident = Actors.FromObject(character, true, false, false);
            if (!_glamourer._stateManager.TryGetValue(ident, out var design))
                design = new ActiveDesign(ident, character.Address);

            return design.CreateOldBase64();
        }

        private string? GetAllCustomization(string characterName)
        {
            foreach (var gameObject in _objectTable)
            {
                if (gameObject.Name.ToString() == characterName)
                    return GetAllCustomization(gameObject as Character);
            }

            return null;
        }
    }
}
