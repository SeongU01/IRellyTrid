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
        isPlaying = true;
    }
    
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

    protected void Success()
    {
        Finish(true);
    }
    private void Finish(bool success)
    {
        if (!isPlaying)
            return;

        isPlaying = false;
        OnFinished?.Invoke(new MiniGameResult(success));
    }
    protected abstract void OnStart();
    protected virtual void OnEnd()
    {
    }
}
