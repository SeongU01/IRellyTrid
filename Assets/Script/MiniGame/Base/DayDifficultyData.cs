using UnityEngine;

[System.Serializable]
public class DayDifficultyData
{
    [SerializeField, Min(1)]
    private int startDay = 1;

    public int StartDay => Mathf.Max(1, startDay);
}
