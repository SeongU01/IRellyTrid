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
    private float nextDayStudyMultiplier = 1f;
    private MiniGameFeedbackEffect feedbackEffect;
    private float finishFeedbackEndsAt;

    // getter
    public bool IsPlaying => isPlaying;
    public int CurrentDay { get; private set; } = 1;
    public int DifficultyDay { get; private set; } = 1;
    public MiniGameAssetVariant AssetVariant { get; private set; } =
        MiniGameAssetVariant.Base;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected int timeLimit;
    protected string commandText;

    public virtual void Init(MiniGameData data)
    {
        isPlaying = false;
        currentData = data;
        studyRewardOverride = null;
        nextDayStudyMultiplier = 1f;
        finishFeedbackEndsAt = 0f;
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

    public void SetAssetVariant(MiniGameAssetVariant assetVariant)
    {
        AssetVariant = assetVariant;
    }
    protected void SuccessWithStudyReward(int studyReward)
    {
        studyRewardOverride = Mathf.Max(0, studyReward);
        Finish(MiniGameEndReason.Success);
    }
    protected void SuccessWithNextDayStudyMultiplier(float multiplier)
    {
        studyRewardOverride = 0;
        nextDayStudyMultiplier = Mathf.Max(1f, multiplier);
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
    protected void ShowCorrectFeedback()
    {
        float duration = GetFeedbackEffect().PlayCorrect();
        finishFeedbackEndsAt = duration > 0f
            ? Time.time + duration
            : 0f;
    }
    protected void ShowWrongFeedback()
    {
        GameAudioManager.PlayMistake();
        float duration = GetFeedbackEffect().PlayWrong();
        finishFeedbackEndsAt = duration > 0f
            ? Time.time + duration
            : 0f;
    }
    private void Finish(MiniGameEndReason endReason)
    {
        if (!isPlaying)
            return;

        float playDuration = Mathf.Max(0f, Time.time - playStartedAt);
        isPlaying = false;
        MiniGameResult result = new MiniGameResult(
            endReason,
            currentData,
            CurrentDay,
            playDuration,
            studyRewardOverride,
            nextDayStudyMultiplier);

        if (endReason == MiniGameEndReason.Success)
        {
            GetFeedbackEffect().PlayClear(() => CompleteFinish(result));
            return;
        }

        if (endReason == MiniGameEndReason.Failure ||
            endReason == MiniGameEndReason.Timeout)
        {
            GameAudioManager.PlayFailure();
            GetFeedbackEffect().PlayFailure(
                endReason == MiniGameEndReason.Timeout,
                () => CompleteFinish(result));
            return;
        }

        float remainingFeedbackTime = Mathf.Max(
            0f,
            finishFeedbackEndsAt - Time.time);

        if (remainingFeedbackTime > 0f)
        {
            GetFeedbackEffect().InvokeAfter(
                remainingFeedbackTime,
                () => CompleteFinish(result));
            return;
        }

        CompleteFinish(result);
    }
    virtual protected void OnEnd()
    {
    }
    private void CompleteFinish(MiniGameResult result)
    {
        OnEnd();
        OnFinished?.Invoke(result);
    }
    private MiniGameFeedbackEffect GetFeedbackEffect()
    {
        if (feedbackEffect == null)
        {
            feedbackEffect = GetComponent<MiniGameFeedbackEffect>();
            if (feedbackEffect == null)
                feedbackEffect = gameObject.AddComponent<MiniGameFeedbackEffect>();
        }

        feedbackEffect.SetDay(CurrentDay);

        return feedbackEffect;
    }
}
