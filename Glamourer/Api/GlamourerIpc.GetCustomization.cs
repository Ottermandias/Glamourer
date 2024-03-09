using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Designs;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Actors;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelGetAllCustomization                    = "Glamourer.GetAllCustomization";
    public const string LabelGetAllCustomizationFromCharacter       = "Glamourer.GetAllCustomizationFromCharacter";
    public const string LabelGetAllCustomizationLocked              = "Glamourer.GetAllCustomizationLocked";
    public const string LabelGetAllCustomizationFromLockedCharacter = "Glamourer.GetAllCustomizationFromLockedCharacter";

    private readonly FuncProvider<string, string?>           _getAllCustomizationProvider;
    private readonly FuncProvider<string, uint, string?>     _getAllCustomizationLockedProvider;
    private readonly FuncProvider<Character?, string?>       _getAllCustomizationFromCharacterProvider;
    private readonly FuncProvider<Character?, uint, string?> _getAllCustomizationFromLockedCharacterProvider;

    public static FuncSubscriber<string, string?> GetAllCustomizationSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelGetAllCustomization);

    public static FuncSubscriber<Character?, string?> GetAllCustomizationFromCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelGetAllCustomizationFromCharacter);

    public static FuncSubscriber<string, uint, string?> GetAllCustomizationLockedSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelGetAllCustomizationLocked);

    public static FuncSubscriber<Character?, uint, string?> GetAllCustomizationFromLockedCharacterSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelGetAllCustomizationFromLockedCharacter);

    public string? GetAllCustomization(string characterName)
        => GetCustomization(FindActors(characterName), 0);

    public string? GetAllCustomization(string characterName, uint lockCode)
        => GetCustomization(FindActors(characterName), lockCode);

    public string? GetAllCustomizationFromCharacter(Character? character)
        => GetCustomization(FindActors(character), 0);

    public string? GetAllCustomizationFromCharacter(Character? character, uint lockCode)
        => GetCustomization(FindActors(character), lockCode);

    private string? GetCustomization(IEnumerable<ActorIdentifier> actors, uint lockCode)
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
        if (!state.CanUnlock(lockCode))
            return null;

        return _designConverter.ShareBase64(state, ApplicationRules.AllWithConfig(_config));
    }
}
