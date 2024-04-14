using Dalamud.Plugin;
using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Enums;

namespace Glamourer.Api.IpcSubscribers;

/// <inheritdoc cref="IGlamourerApiItems.SetItem"/>
public sealed class SetItem(DalamudPluginInterface pi)
    : FuncSubscriber<int, byte, ulong, byte, uint, ulong, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(SetItem)}";

    /// <inheritdoc cref="IGlamourerApiItems.SetItem"/>
    public GlamourerApiEc Invoke(int objectIndex, EquipSlot slot, ulong itemId, byte stain, uint key = 0, ApplyFlag flags = ApplyFlag.Once)
        => (GlamourerApiEc)Invoke(objectIndex, (byte)slot, itemId, stain, key, (ulong)flags);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, byte, ulong, byte, uint, ulong, int> Provider(DalamudPluginInterface pi, IGlamourerApiItems api)
        => new(pi, Label, (a, b, c, d, e, f) => (int)api.SetItem(a, (ApiEquipSlot)b, c, d, e, (ApplyFlag)f));
}

/// <inheritdoc cref="IGlamourerApiItems.SetItemName"/>
public sealed class SetItemName(DalamudPluginInterface pi)
    : FuncSubscriber<string, byte, ulong, byte, uint, ulong, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(SetItemName)}";

    /// <inheritdoc cref="IGlamourerApiItems.SetItem"/>
    public GlamourerApiEc Invoke(string objectName, EquipSlot slot, ulong itemId, byte stain, uint key = 0, ApplyFlag flags = ApplyFlag.Once)
        => (GlamourerApiEc)Invoke(objectName, (byte)slot, itemId, stain, key, (ulong)flags);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, byte, ulong, byte, uint, ulong, int> Provider(DalamudPluginInterface pi, IGlamourerApiItems api)
        => new(pi, Label, (a, b, c, d, e, f) => (int)api.SetItemName(a, (ApiEquipSlot)b, c, d, e, (ApplyFlag)f));
}
