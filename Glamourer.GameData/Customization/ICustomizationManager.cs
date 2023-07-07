using System.Collections.Generic;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization;

public interface ICustomizationManager
{
    public IReadOnlyList<Race>    Races   { get; }
    public IReadOnlyList<SubRace> Clans   { get; }
    public IReadOnlyList<Gender>  Genders { get; }

    public CustomizationSet GetList(SubRace race, Gender gender);

    public ImGuiScene.TextureWrap GetIcon(uint iconId);
    public string                 GetName(CustomName name);
}
