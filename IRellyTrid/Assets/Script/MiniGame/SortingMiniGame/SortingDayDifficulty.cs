using UnityEngine;

[System.Serializable]
public class SortingDayDifficulty : DayDifficultyData
{
    [Header("Categories")]
    [Min(2)] public int categoryCount = 2;

    [Header("Goal")]
    [Min(1)] public int requiredSortCount = 8;

    [Header("Rules")]
    [Min(1)] public int allowedMistakes = 3;
}
