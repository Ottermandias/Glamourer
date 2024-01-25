using Glamourer.Designs.Links;
using Glamourer.GameData;
using Glamourer.State;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public readonly record struct ApplySettings(
    uint Key = 0,
    StateSource Source = StateSource.Manual,
    bool RespectManual = false,
    bool FromJobChange = false,
    bool UseSingleSource = false);

public interface IDesignEditor
{
    /// <summary> Change a customization value. </summary>
    public void ChangeCustomize(object data, CustomizeIndex idx, CustomizeValue value, ApplySettings settings = default);

    /// <summary> Change an entire customize array according to the given flags. </summary>
    public void ChangeEntireCustomize(object data, in CustomizeArray customizeInput, CustomizeFlag apply, ApplySettings settings = default);

    /// <summary> Change a customize parameter. </summary>
    public void ChangeCustomizeParameter(object data, CustomizeParameterFlag flag, CustomizeParameterValue v, ApplySettings settings = default);

    /// <summary> Change an equipment piece. </summary>
    public void ChangeItem(object data, EquipSlot slot, EquipItem item, ApplySettings settings = default)
        => ChangeEquip(data, slot, item, null, settings);

    /// <summary> Change the stain for any equipment piece. </summary>
    public void ChangeStain(object data, EquipSlot slot, StainId stain, ApplySettings settings = default)
        => ChangeEquip(data, slot, null, stain, settings);

    /// <summary> Change an equipment piece and its stain at the same time. </summary>
    public void ChangeEquip(object data, EquipSlot slot, EquipItem? item, StainId? stain, ApplySettings settings = default);

    /// <summary> Change the crest visibility for any equipment piece. </summary>
    public void ChangeCrest(object data, CrestFlag slot, bool crest, ApplySettings settings = default);

    /// <summary> Change the bool value of one of the meta flags. </summary>
    public void ChangeMetaState(object data, MetaIndex slot, bool value, ApplySettings settings = default);

    /// <summary> Change all values applies from the given design. </summary>
    public void ApplyDesign(object data, MergedDesign design, ApplySettings settings = default);

    /// <summary> Change all values applies from the given design. </summary>
    public void ApplyDesign(object data, DesignBase design, ApplySettings settings = default);
}
