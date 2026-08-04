using UnityEngine;

[System.Serializable]
public class TypingWordDayDifficulty : DayDifficultyData
{
    [Min(1)] public int totalQuestionCount = 10;
    [Min(1)] public int allowedMistakes = 3;
    public string[] words;
}
