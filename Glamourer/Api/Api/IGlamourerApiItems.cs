using Glamourer.Api.Enums;

namespace Glamourer.Api.Api;

public interface IGlamourerApiItems
{
    public GlamourerApiEc SetItem(int objectIndex, ApiEquipSlot apiSlot, ulong itemId, byte stain, uint key, ApplyFlag flags);
    public GlamourerApiEc SetItemName(string objectName, ApiEquipSlot slot, ulong itemId, byte stain, uint key, ApplyFlag flags);
}
