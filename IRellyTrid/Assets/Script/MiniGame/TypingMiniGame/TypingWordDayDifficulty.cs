using UnityEngine;

[System.Serializable]
public class TypingWordDayDifficulty : DayDifficultyData
{
    [Min(1)] public int totalQuestionCount = 10;
    public string[] words;
}
