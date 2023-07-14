using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Glamourer.Unlocks;

public readonly record struct UnlockRequirements(uint Quest1, uint Quest2, uint Achievement, ushort State, ItemUnlockManager.UnlockType Type)
{
    public override string ToString()
    {
        return Type switch
        {
            ItemUnlockManager.UnlockType.Quest1                            => $"Quest {Quest1}",
            ItemUnlockManager.UnlockType.Quest1 | ItemUnlockManager.UnlockType.Quest2        => $"Quests {Quest1} & {Quest2}",
            ItemUnlockManager.UnlockType.Achievement                                         => $"Achievement {Achievement}",
            ItemUnlockManager.UnlockType.Quest1 | ItemUnlockManager.UnlockType.Achievement                     => $"Quest {Quest1} & Achievement {Achievement}",
            ItemUnlockManager.UnlockType.Quest1 | ItemUnlockManager.UnlockType.Quest2 | ItemUnlockManager.UnlockType.Achievement => $"Quests {Quest1} & {Quest2}, Achievement {Achievement}",
            ItemUnlockManager.UnlockType.Cabinet                                             => $"Cabinet {Quest1}",
            _                                                              => string.Empty,
        };
    }

    public unsafe bool IsUnlocked(ItemUnlockManager manager)
    {
        if (Type == 0)
            return true;

        var uiState = UIState.Instance();
        if (uiState == null)
            return false;

        bool CheckQuest(uint quest)
            => uiState->IsUnlockLinkUnlockedOrQuestCompleted(quest);

        bool CheckAchievement(uint achievement)
            => uiState->Achievement.IsLoaded() && uiState->Achievement.IsComplete((int) achievement);

        return Type switch
        {
            ItemUnlockManager.UnlockType.Quest1                                       => CheckQuest(Quest1),
            ItemUnlockManager.UnlockType.Quest1 | ItemUnlockManager.UnlockType.Quest2 => CheckQuest(Quest1) && CheckQuest(Quest2),
            ItemUnlockManager.UnlockType.Achievement                                  => CheckAchievement(Achievement),
            ItemUnlockManager.UnlockType.Quest1 | ItemUnlockManager.UnlockType.Achievement              => CheckQuest(Quest1) && CheckAchievement(Achievement),
            ItemUnlockManager.UnlockType.Quest1 | ItemUnlockManager.UnlockType.Quest2 | ItemUnlockManager.UnlockType.Achievement => CheckQuest(Quest1)
             && CheckQuest(Quest2)
             && CheckAchievement(Achievement),
            ItemUnlockManager.UnlockType.Cabinet => uiState->Cabinet.IsCabinetLoaded() && uiState->Cabinet.IsItemInCabinet((int)Quest1),
            _                                    => false,
        };
    }
}
