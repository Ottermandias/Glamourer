using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;

namespace Glamourer.Api
{
    public class GlamourerIpc : IDisposable
    {
        public const string LabelProviderApiVersion = "Glamourer.ApiVersion";
        public const string LabelProviderGetCharacterCustomization = "Glamourer.GetCharacterCustomization";
        public const string LabelProviderApplyCharacterCustomization = "Glamourer.ApplyCharacterCustomization";
        private readonly ClientState clientState;
        private readonly ObjectTable objectTable;
        private readonly DalamudPluginInterface pluginInterface;

        internal ICallGateProvider<string>? ProviderGetCharacterCustomization;
        internal ICallGateProvider<string, string, object>? ProviderApplyCharacterCustomization;
        internal ICallGateProvider<int>? ProviderGetApiVersion;

        public GlamourerIpc(ClientState clientState, ObjectTable objectTable, DalamudPluginInterface pluginInterface)
        {
            this.clientState = clientState;
            this.objectTable = objectTable;
            this.pluginInterface = pluginInterface;

            InitializeProviders();
        }

        public void Dispose()
        {
            DisposeProviders();
        }

        private void DisposeProviders()
        {
            ProviderApplyCharacterCustomization?.UnregisterFunc();
            ProviderGetCharacterCustomization?.UnregisterAction();
            ProviderGetApiVersion?.UnregisterFunc();
        }

        private void InitializeProviders()
        {
            try
            {
                ProviderGetApiVersion = pluginInterface.GetIpcProvider<int>(LabelProviderApiVersion);
                ProviderGetApiVersion.RegisterFunc(GetApiVersion);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApiVersion}.");
            }

            try
            {
                ProviderGetCharacterCustomization = pluginInterface.GetIpcProvider<string>(LabelProviderGetCharacterCustomization);
                ProviderGetCharacterCustomization.RegisterFunc(GetCharacterCustomization);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyCharacterCustomization}.");
            }

            try
            {
                ProviderApplyCharacterCustomization = pluginInterface.GetIpcProvider<string, string, object>(LabelProviderApplyCharacterCustomization);
                ProviderApplyCharacterCustomization.RegisterAction((customization, characterName) => ApplyCharacterCustomization(customization, characterName));
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyCharacterCustomization}.");
            }
        }

        private int GetApiVersion() => 0;

        private void ApplyCharacterCustomization(string customization, string characterName)
        {
            var save = CharacterSave.FromString(customization);
            foreach (var gameObject in objectTable)
            {
                if (gameObject.Name.ToString() == characterName)
                {
                    var player = (Character)gameObject;
                    save.Apply(player);
                    Glamourer.Penumbra.UpdateCharacters(player, null);
                }
            }
        }

        private string GetCharacterCustomization()
        {
            CharacterSave save = new CharacterSave();
            save.LoadCharacter(clientState.LocalPlayer!);
            return save.ToBase64();
        }
    }
}
