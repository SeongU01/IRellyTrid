using UnityEngine;

[System.Serializable]
public class SortingDayDifficulty : DayDifficultyData
{
    [Header("Categories")]
    [Tooltip("Categories 배열에서 이 일차에 사용할 카테고리의 인덱스 목록")]
    public int[] categoryIndices = new int[0];

    [Header("Goal")]
    [Min(1)] public int requiredSortCount = 8;

    [Header("Rules")]
    [Min(1)] public int allowedMistakes = 3;
}
