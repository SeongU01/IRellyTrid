using System;
using UnityEngine;

public abstract class MiniGameBase : MonoBehaviour
{
    public event Action<MiniGameResult> OnFinished;

    private bool isPlaying;

    // getter
    public bool IsPlaying => isPlaying;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public virtual void Init()
    {
        isPlaying = false;
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
        OnFinished?.Invoke(new MiniGameResult(success));
        OnEnd();
    }
    virtual protected void OnEnd()
    {
    }
}
