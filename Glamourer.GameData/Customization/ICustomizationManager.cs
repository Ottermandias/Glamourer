using System.Collections.Generic;
using Dalamud.Interface.Internal;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization;

public interface ICustomizationManager
{
    public IReadOnlyList<Race>    Races   { get; }
    public IReadOnlyList<SubRace> Clans   { get; }
    public IReadOnlyList<Gender>  Genders { get; }

    public CustomizationSet GetList(SubRace race, Gender gender);

    public IDalamudTextureWrap GetIcon(uint iconId);
    public string              GetName(CustomName name);
}
