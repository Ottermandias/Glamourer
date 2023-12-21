using System.Collections.Generic;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization;

public class CustomizationManager : ICustomizationManager
{
    private static CustomizationOptions? _options;

    private CustomizationManager()
    { }

    public static ICustomizationManager Create(ITextureProvider textures, IDataManager gameData, IPluginLog log, NpcCustomizeSet npcCustomizeSet)
    {
        _options ??= new CustomizationOptions(textures, gameData, log, npcCustomizeSet);
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

    public IDalamudTextureWrap GetIcon(uint iconId)
        => _options!.GetIcon(iconId);

    public string GetName(CustomName name)
        => _options!.GetName(name);
}
