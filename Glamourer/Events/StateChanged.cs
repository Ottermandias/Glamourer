using Glamourer.Interop.Structs;
using Glamourer.State;
using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when a Design is edited in any way.
/// <list type="number">
///     <item>Parameter is the type of the change </item>
///     <item>Parameter is the changed saved state. </item>
///     <item>Parameter is the existing actors using this saved state. </item>
///     <item>Parameter is any additional data depending on the type of change. </item>
/// </list>
/// </summary>
public sealed class StateChanged()
    : EventWrapper<StateChanged.Type, StateChanged.Source, ActorState, ActorData, object?, StateChanged.Priority>(nameof(StateChanged))
{
    public enum Type
    {
        /// <summary> A characters saved state had the model id changed. This means everything may have changed. Data is the old model id and the new model id. [(uint, uint)] </summary>
        Model,

        /// <summary> A characters saved state had multiple customization values changed. TData is the old customize array and the applied changes. [(Customize, CustomizeFlag)] </summary>
        EntireCustomize,

        /// <summary> A characters saved state had a customization value changed. Data is the old value, the new value and the type. [(CustomizeValue, CustomizeValue, CustomizeIndex)]. </summary>
        Customize,

        /// <summary> A characters saved state had an equipment piece changed. Data is the old value, the new value and the slot [(EquipItem, EquipItem, EquipSlot)]. </summary>
        Equip,

        /// <summary> A characters saved state had its weapons changed. Data is the old mainhand, the old offhand, the new mainhand and the new offhand [(EquipItem, EquipItem, EquipItem, EquipItem)]. </summary>
        Weapon,

        /// <summary> A characters saved state had a stain changed. Data is the old stain id, the new stain id and the slot [(StainId, StainId, EquipSlot)]. </summary>
        Stain,

        /// <summary> A characters saved state had a crest visibility changed. Data is the old crest visibility, the new crest visibility and the slot [(bool, bool, EquipSlot)]. </summary>
        Crest,

        /// <summary> A characters saved state had a design applied. This means everything may have changed. Data is the applied design. [DesignBase] </summary>
        Design,

        /// <summary> A characters saved state had its state reset to its game values. This means everything may have changed. Data is null. </summary>
        Reset,

        /// <summary> A characters saved state had a meta toggle changed. Data is the old stain id, the new stain id and the slot [(StainId, StainId, EquipSlot)]. </summary>
        Other,
    }

    public enum Source : byte
    {
        Game,
        Manual,
        Fixed,
        Ipc,
    }

    public enum Priority
    {
        GlamourerIpc = int.MinValue,
    }
}
