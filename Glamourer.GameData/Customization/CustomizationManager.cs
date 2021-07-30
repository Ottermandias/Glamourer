using System.Collections.Generic;
using Dalamud.Plugin;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization
{
    public class CustomizationManager : ICustomizationManager
    {
        private static CustomizationOptions? _options;

        private CustomizationManager()
        { }

        public static ICustomizationManager Create(DalamudPluginInterface pi)
        {
            _options ??= new CustomizationOptions(pi);
            return new CustomizationManager();
        }

        public IReadOnlyList<Race> Races
            => CustomizationOptions.Races;

        public IReadOnlyList<SubRace> Clans
            => CustomizationOptions.Clans;

        public IReadOnlyList<Gender> Genders
            => CustomizationOptions.Genders;

        public CustomizationSet GetList(SubRace clan, Gender gender)
            => _options!.GetList(clan, gender);

        public ImGuiScene.TextureWrap GetIcon(uint iconId)
            => _options!.GetIcon(iconId);
    }
}
