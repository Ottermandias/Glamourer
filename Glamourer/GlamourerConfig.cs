using System.Collections.Generic;
using Dalamud.Configuration;

namespace Glamourer
{
    public class GlamourerConfig : IPluginConfiguration
    {
        public struct FixedDesign
        {
            public string Name;
            public string Path;
            public uint   JobGroups;
            public bool   Enabled;
        }

        public int Version { get; set; } = 1;

        public const uint DefaultCustomizationColor = 0xFFC000C0;
        public const uint DefaultStateColor         = 0xFF00C0C0;
        public const uint DefaultEquipmentColor     = 0xFF00C000;

        public bool FoldersFirst      { get; set; } = false;
        public bool ColorDesigns      { get; set; } = true;
        public bool ShowLocks         { get; set; } = true;
        public bool AttachToPenumbra  { get; set; } = true;
        public bool ApplyFixedDesigns { get; set; } = true;

        public uint CustomizationColor { get; set; } = DefaultCustomizationColor;
        public uint StateColor         { get; set; } = DefaultStateColor;
        public uint EquipmentColor     { get; set; } = DefaultEquipmentColor;

        public List<FixedDesign> FixedDesigns { get; set; } = new();

        public void Save()
            => Dalamud.PluginInterface.SavePluginConfig(this);

        public static GlamourerConfig Load()
        {
            if (Dalamud.PluginInterface.GetPluginConfig() is GlamourerConfig config)
                return config;

            config = new GlamourerConfig();
            config.Save();
            return config;
        }
    }
}
