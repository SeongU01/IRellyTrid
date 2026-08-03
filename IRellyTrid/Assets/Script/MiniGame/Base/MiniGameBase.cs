using System;
using UnityEngine;

public abstract class MiniGameBase : MonoBehaviour
{
    public event Action<MiniGameResult> OnFinished;

    private bool isPlaying;

    // getter
    public bool IsPlaying => isPlaying;
    public int CurrentDay { get; private set; } = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected int timeLimit;
    protected string commandText;

    public virtual void Init(MiniGameData data)
    {
        isPlaying = false;
        timeLimit = (int)data.timeLimit;
        commandText = data.commandText;
    }

    public void Init(MiniGameData data, int currentDay)
    {
        CurrentDay = Mathf.Max(1, currentDay);

        // 기존 Init 오버라이드가 있는 미니게임도 그대로 호출한다.
        Init(data);
    }
    abstract protected void OnStart();
    public void Play()
    {
        if (isPlaying)
            return;

        isPlaying = true;
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
        Finish(true);
    }
    virtual protected void Fail()
    {
        Finish(false);
    }
    public void Timeout()
    {
        Finish(false);
    }
    private void Finish(bool success)
    {
        if (!isPlaying)
            return;
    
        isPlaying = false;
        OnEnd();
        OnFinished?.Invoke(new MiniGameResult(success));
    }
    virtual protected void OnEnd()
    {
    }
}
