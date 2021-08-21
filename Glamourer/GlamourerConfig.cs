using Dalamud.Configuration;

namespace Glamourer
{
    public class GlamourerConfig : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public const uint DefaultCustomizationColor = 0xFFC000C0;
        public const uint DefaultStateColor         = 0xFF00C0C0;
        public const uint DefaultEquipmentColor     = 0xFF00C000;

        public bool FoldersFirst { get; set; } = false;
        public bool ColorDesigns { get; set; } = true;
        public bool ShowLocks    { get; set; } = true;

        public uint CustomizationColor { get; set; } = DefaultCustomizationColor;
        public uint StateColor         { get; set; } = DefaultStateColor;
        public uint EquipmentColor     { get; set; } = DefaultEquipmentColor;

        public void Save()
            => Glamourer.PluginInterface.SavePluginConfig(this);

        public static GlamourerConfig Create()
        {
            var config = Glamourer.PluginInterface.GetPluginConfig() as GlamourerConfig;
            if (config == null)
            {
                config = new GlamourerConfig();
                Glamourer.PluginInterface.SavePluginConfig(config);
            }

            return config;
        }
    }
}
