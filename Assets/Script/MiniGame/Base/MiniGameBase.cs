using System;
using UnityEngine;

public abstract class MiniGameBase : MonoBehaviour
{
    public event Action<MiniGameResult> OnFinished;
    public event Action OnTimerResetRequested;

    private bool isPlaying;
    private MiniGameData currentData;
    private float playStartedAt;
    private int? studyRewardOverride;

    // getter
    public bool IsPlaying => isPlaying;
    public int CurrentDay { get; private set; } = 1;
    public int DifficultyDay { get; private set; } = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected int timeLimit;
    protected string commandText;

    public virtual void Init(MiniGameData data)
    {
        isPlaying = false;
        currentData = data;
        studyRewardOverride = null;
        timeLimit = (int)data.timeLimit;
        commandText = data.commandText;
    }

    public void Init(MiniGameData data, int currentDay)
    {
        Init(data, currentDay, currentDay, data.timeLimit);
    }

    public void Init(
        MiniGameData data,
        int currentDay,
        int difficultyDay,
        float effectiveTimeLimit)
    {
        CurrentDay = Mathf.Max(1, currentDay);
        DifficultyDay = Mathf.Max(1, difficultyDay);

        // 기존 Init 오버라이드가 있는 미니게임도 그대로 호출한다.
        Init(data);
        timeLimit = Mathf.CeilToInt(Mathf.Max(0.1f, effectiveTimeLimit));
    }
    abstract protected void OnStart();
    public void Play()
    {
        if (isPlaying)
            return;

        isPlaying = true;
        playStartedAt = Time.time;
        OnStart();
    }

    public void Stop()
    {
        if (!isPlaying)
            return;

        isPlaying = false;
        OnEnd();
    }

    virtual protected void Success()
    {
        Finish(MiniGameEndReason.Success);
    }
    protected void SuccessWithStudyReward(int studyReward)
    {
        studyRewardOverride = Mathf.Max(0, studyReward);
        Finish(MiniGameEndReason.Success);
    }
    virtual protected void Fail()
    {
        Finish(MiniGameEndReason.Failure);
    }
    public virtual void Timeout()
    {
        Finish(MiniGameEndReason.Timeout);
    }
    protected void RequestTimerReset()
    {
        if (isPlaying)
            OnTimerResetRequested?.Invoke();
    }
    public void CompleteAsSuccessForDebug()
    {
        Success();
    }
    private void Finish(MiniGameEndReason endReason)
    {
        if (!isPlaying)
            return;

        float playDuration = Mathf.Max(0f, Time.time - playStartedAt);
        isPlaying = false;
        OnEnd();
        OnFinished?.Invoke(new MiniGameResult(
            endReason,
            currentData,
            CurrentDay,
            playDuration,
            studyRewardOverride));
    }
    virtual protected void OnEnd()
    {
    }
}
