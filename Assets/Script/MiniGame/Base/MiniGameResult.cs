using UnityEngine;

public enum MiniGameEndReason
{
    Success,
    Failure,
    Timeout,
    Cancelled,
    GameOver
}

public class MiniGameResult
{
    public bool Success => EndReason == MiniGameEndReason.Success;
    public MiniGameEndReason EndReason { get; }
    public MiniGameData Data { get; }
    public string MiniGameName => Data != null ? Data.name : string.Empty;
    public bool IsBonus => Data != null && Data.isBonus;
    public int Day { get; }
    public float PlayDuration { get; }
    public int? StudyRewardOverride { get; }
    public float NextDayStudyMultiplier { get; }

    public MiniGameResult(
        MiniGameEndReason endReason,
        MiniGameData data,
        int day,
        float playDuration,
        int? studyRewardOverride = null,
        float nextDayStudyMultiplier = 1f)
    {
        EndReason = endReason;
        Data = data;
        Day = day;
        PlayDuration = playDuration;
        StudyRewardOverride = studyRewardOverride;
        NextDayStudyMultiplier = Mathf.Max(1f, nextDayStudyMultiplier);
    }
}
