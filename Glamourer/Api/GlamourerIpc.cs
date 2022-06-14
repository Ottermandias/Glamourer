using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glamourer.Api
{
    public class GlamourerIpc : IDisposable
    {
        public const string LabelProviderGetCharacterCustomization = "Glamourer.GetCharacterCustomization";
        public const string LabelProviderApplyCharacterCustomization = "Glamourer.ApplyCharacterCustomization";

        private readonly ObjectTable objectTable;
        private readonly DalamudPluginInterface pi;

        internal ICallGateProvider<string>? ProviderGetCharacterCustomization;
        internal ICallGateProvider<string, string, object>? ProviderApplyCharacterCustomization;

        public GlamourerIpc(ObjectTable objectTable, DalamudPluginInterface pi)
        {
            this.objectTable = objectTable;
            this.pi = pi;

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
        }

        private void InitializeProviders()
        {
            try
            {
                ProviderGetCharacterCustomization = pi.GetIpcProvider<string>(LabelProviderGetCharacterCustomization);
                ProviderGetCharacterCustomization.RegisterFunc(GetCharacterCustomization);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyCharacterCustomization}.");
            }

            try
            {
                ProviderApplyCharacterCustomization = pi.GetIpcProvider<string, string, object>(LabelProviderApplyCharacterCustomization);
                ProviderApplyCharacterCustomization.RegisterAction((customization, characterName) => ApplyCharacterCustomization(customization, characterName));
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApplyCharacterCustomization}.");
            }
        }

        private void ApplyCharacterCustomization(string customization, string characterName)
        {
            var save = CharacterSave.FromString(customization);
            foreach (var gameObject in objectTable)
            {
                if (gameObject.Name.ToString() == characterName)
                {
                    save.Apply((Character)gameObject);
                }
            }
        }

        private string GetCharacterCustomization()
        {
            CharacterSave save = new CharacterSave();
            save.LoadCharacter((Character)Glamourer.GetPlayer("self")!);
            return save.ToBase64();
        }
    }
}
