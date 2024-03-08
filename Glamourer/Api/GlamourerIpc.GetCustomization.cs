using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Designs;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelGetAllCustomization              = "Glamourer.GetAllCustomization";
    public const string LabelGetAllCustomizationFromCharacter = "Glamourer.GetAllCustomizationFromCharacter";

    private readonly FuncProvider<string, string?>     _getAllCustomizationProvider;
    private readonly FuncProvider<Character?, string?> _getAllCustomizationFromCharacterProvider;

    public static FuncSubscriber<string, string?> GetAllCustomizationSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelGetAllCustomization);

    public static FuncSubscriber<Character?, string?> GetAllCustomizationFromCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelGetAllCustomizationFromCharacter);

    public string? GetAllCustomization(string characterName)
        => GetCustomization(FindActors(characterName));

    public string? GetAllCustomizationFromCharacter(Character? character)
        => GetCustomization(FindActors(character));

    private string? GetCustomization(IEnumerable<ActorIdentifier> actors)
    {
        var actor = actors.FirstOrDefault(ActorIdentifier.Invalid);
        if (!actor.IsValid)
            return null;

        if (!_stateManager.TryGetValue(actor, out var state))
        {
            _objects.Update();
            if (!_objects.TryGetValue(actor, out var data) || !data.Valid)
                return null;
            if (!_stateManager.GetOrCreate(actor, data.Objects[0], out state))
                return null;
        }

        return _designConverter.ShareBase64(state, ApplicationRules.AllWithConfig(_config));
    }
}
