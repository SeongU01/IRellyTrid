using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiniGameTimerUrgencyEffect : MonoBehaviour
{
    private const float WarningThreshold = 5f;
    private const float CriticalThreshold = 3f;

    private TMP_Text timerText;
    private RectTransform timerRect;
    private Vector2 basePosition;
    private Vector3 baseScale;
    private Color baseColor;
    private float remainingTime;
    private bool active;
    private MiniGameManager manager;

    private void Awake()
    {
        manager = GetComponent<MiniGameManager>();
    }

    private void LateUpdate()
    {
        if (!active || timerRect == null ||
            (manager != null && manager.IsPaused))
        {
            RestoreTimerTransform();
            return;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 4f) + 1f) * 0.5f;
        float shake = Mathf.Sin(Time.unscaledTime * Mathf.PI * 18f) * 2f;
        timerRect.anchoredPosition = basePosition + Vector2.right * shake;
        timerRect.localScale = baseScale * Mathf.Lerp(1f, 1.1f, pulse);
        timerText.color = remainingTime <= CriticalThreshold
            ? new Color(0.85f, 0.08f, 0.08f, baseColor.a)
            : baseColor;
    }

    public void Configure(TMP_Text target)
    {
        if (target == null || timerText == target)
            return;

        timerText = target;
        timerRect = target.rectTransform;
        basePosition = timerRect.anchoredPosition;
        baseScale = timerRect.localScale;
        baseColor = timerText.color;
    }

    public void SetRemainingTime(float time)
    {
        if (timerText == null)
            return;

        remainingTime = Mathf.Max(0f, time);
        active = remainingTime > 0f && remainingTime <= WarningThreshold;

        if (!active)
            RestoreTimerVisuals();
    }

    public void Clear()
    {
        active = false;
        RestoreTimerVisuals();
    }

    private void RestoreTimerTransform()
    {
        if (timerRect == null)
            return;

        timerRect.anchoredPosition = basePosition;
        timerRect.localScale = baseScale;
    }

    private void RestoreTimerVisuals()
    {
        RestoreTimerTransform();

        if (timerText != null)
            timerText.color = baseColor;
    }
}
