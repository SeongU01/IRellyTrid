using UnityEngine;

[System.Serializable]
public class ArrowKeyDayDifficulty : DayDifficultyData
{
    [Header("Board")]
    public Vector2Int boardSize = new Vector2Int(8, 6);

    [Header("Arrow Count")]
    [Min(1)] public int minArrowCount = 6;
    [Min(1)] public int maxArrowCount = 7;

    [Tooltip("경로에 반드시 포함할 최소 방향 전환 횟수")]
    [Min(0)] public int minTurnCount = 4;

    [Header("Rules")]
    [Min(1)] public int stageCount = 3;
    [Min(1)] public int allowedMistakes = 3;

    [Header("Layout")]
    public Vector2 cellSpacing = new Vector2(76f, 76f);
}
