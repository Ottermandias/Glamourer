using System;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace Penumbra.PlayerWatch;

public static class CharacterFactory
{
    private static ConstructorInfo? _characterConstructor;

    private static void Initialize()
    {
        _characterConstructor ??= typeof(Character).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[]
        {
            typeof(IntPtr),
        }, null)!;
    }

    private static Character Character(IntPtr address)
    {
        Initialize();
        return (Character)_characterConstructor?.Invoke(new object[]
        {
            address,
        })!;
    }

    public static Character? Convert(GameObject? actor)
    {
        if (actor == null)
            return null;

        return actor switch
        {
            PlayerCharacter p => p,
            BattleChara b     => b,
            _ => actor.ObjectKind switch
            {
                ObjectKind.BattleNpc => Character(actor.Address),
                ObjectKind.Companion => Character(actor.Address),
                ObjectKind.Retainer  => Character(actor.Address),
                ObjectKind.EventNpc  => Character(actor.Address),
                _                    => null,
            },
        };
    }
}

public static class GameObjectExtensions
{
    public static unsafe uint ModelType(this Character actor)
        => (uint) ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)actor.Address)->CharacterData.ModelCharaId;

    public static unsafe void SetModelType(this Character actor, uint value)
        => ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)actor.Address)->CharacterData.ModelCharaId = (int) value;
}
