using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class CanvasGroupFader : MonoBehaviour
{
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool cancelPreviousFadeOnNewRequest = true;
    [SerializeField] private bool blockInputDuringFade;

    private CanvasGroup canvasGroup;
    private Coroutine activeFade;
    private bool inputStateStored;
    private bool previousBlocksRaycasts;
    private bool previousInteractable;

    public bool IsFading => activeFade != null;
    public float Alpha => GetCanvasGroup().alpha;

    private void Awake()
    {
        GetCanvasGroup();
    }

    private void OnDisable()
    {
        Cancel();
    }

    public void Configure(
        bool cancelPrevious,
        bool blockInput,
        bool useUnscaled)
    {
        cancelPreviousFadeOnNewRequest = cancelPrevious;
        blockInputDuringFade = blockInput;
        useUnscaledTime = useUnscaled;
    }

    public void SetAlpha(float alpha)
    {
        Cancel();
        GetCanvasGroup().alpha = Mathf.Clamp01(alpha);
    }

    public Coroutine FadeIn(float duration)
    {
        return FadeTo(1f, duration);
    }

    public Coroutine FadeOut(float duration)
    {
        return FadeTo(0f, duration);
    }

    public Coroutine FadeTo(float targetAlpha, float duration)
    {
        return StartFade(FadeToRoutine(targetAlpha, duration));
    }

    public Coroutine PlayFadeInOut(
        float fadeInDuration,
        float holdDuration,
        float fadeOutDuration)
    {
        return StartFade(FadeInOutRoutine(
            fadeInDuration,
            holdDuration,
            fadeOutDuration));
    }

    public void Cancel()
    {
        if (activeFade != null)
        {
            StopCoroutine(activeFade);
            activeFade = null;
        }

        RestoreInputState();
    }

    private Coroutine StartFade(IEnumerator fadeRoutine)
    {
        if (activeFade != null)
        {
            if (!cancelPreviousFadeOnNewRequest)
                return null;

            Cancel();
        }

        StoreAndApplyInputState();
        activeFade = StartCoroutine(RunFade(fadeRoutine));
        return activeFade;
    }

    private IEnumerator RunFade(IEnumerator fadeRoutine)
    {
        yield return fadeRoutine;
        activeFade = null;
        RestoreInputState();
    }

    private IEnumerator FadeInOutRoutine(
        float fadeInDuration,
        float holdDuration,
        float fadeOutDuration)
    {
        yield return FadeAlpha(1f, fadeInDuration);
        yield return Wait(holdDuration);
        yield return FadeAlpha(0f, fadeOutDuration);
    }

    private IEnumerator FadeToRoutine(float targetAlpha, float duration)
    {
        yield return FadeAlpha(targetAlpha, duration);
    }

    private IEnumerator FadeAlpha(float targetAlpha, float duration)
    {
        CanvasGroup targetGroup = GetCanvasGroup();
        float startAlpha = targetGroup.alpha;
        float safeDuration = Mathf.Max(0f, duration);

        if (safeDuration <= 0f)
        {
            targetGroup.alpha = Mathf.Clamp01(targetAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += GetDeltaTime();
            targetGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        targetGroup.alpha = Mathf.Clamp01(targetAlpha);
    }

    private IEnumerator Wait(float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private CanvasGroup GetCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        return canvasGroup;
    }

    private void StoreAndApplyInputState()
    {
        if (!blockInputDuringFade)
            return;

        CanvasGroup targetGroup = GetCanvasGroup();
        previousBlocksRaycasts = targetGroup.blocksRaycasts;
        previousInteractable = targetGroup.interactable;
        inputStateStored = true;
        targetGroup.blocksRaycasts = true;
        targetGroup.interactable = false;
    }

    private void RestoreInputState()
    {
        if (!inputStateStored)
            return;

        CanvasGroup targetGroup = GetCanvasGroup();
        targetGroup.blocksRaycasts = previousBlocksRaycasts;
        targetGroup.interactable = previousInteractable;
        inputStateStored = false;
    }
}
