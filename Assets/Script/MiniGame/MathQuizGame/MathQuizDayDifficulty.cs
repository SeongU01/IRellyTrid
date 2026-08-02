using UnityEngine;

[System.Serializable]
public class MathQuizDayDifficulty : DayDifficultyData
{
    [Min(1)] public int totalQuestionCount = 20;

    [Header("Number Range")]
    [Min(1)] public int minNumber = 1;
    [Min(1)] public int maxNumber = 9;

    [Header("Operators")]
    public bool useAddition = true;
    public bool useSubtraction = true;
    public bool useMultiplication = true;
    public bool useDivision = true;
}
