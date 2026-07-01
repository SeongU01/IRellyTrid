using System;
using UnityEngine;

public abstract class MiniGameBase : MonoBehaviour
{
    public event Action<MiniGameResult> OnFinished;

    private bool isPlaying;

    // getter
    public bool IsPlaying => isPlaying;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected int timeLimit;
    protected string commandText;

    public virtual void Init(MiniGameData data)
    {
        isPlaying = false;
        timeLimit = (int)data.timeLimit;
        commandText = data.commandText;
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
