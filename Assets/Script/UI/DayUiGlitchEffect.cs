using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DayUiGlitchEffect : MonoBehaviour
{
    private MiniGameManager miniGameManager;
    private UiGlitchVisuals visuals;
    private Coroutine glitchRoutine;
    private float nextGlitchTime;
    private int scheduledDay = -1;
    private bool wasPlaying;
    private bool forcedGlitchPending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddToMiniGameManager()
    {
        MiniGameManager manager = FindAnyObjectByType<MiniGameManager>();
        if (manager != null &&
            manager.GetComponent<DayUiGlitchEffect>() == null)
        {
            manager.gameObject.AddComponent<DayUiGlitchEffect>();
        }
    }

    private void Awake()
    {
        miniGameManager = GetComponent<MiniGameManager>();
        visuals = new UiGlitchVisuals();
    }

    private void Update()
    {
        bool isPlaying = miniGameManager != null &&
            miniGameManager.IsMiniGamePlaying;
        int day = miniGameManager != null
            ? miniGameManager.CurrentDay
            : 1;

        if (!isPlaying || day < 6 || day > 9)
        {
            if (wasPlaying)
                StopActiveGlitch();

            if (wasPlaying || scheduledDay != day)
                ResetSchedule(day);

            wasPlaying = false;
            return;
        }

        if (!wasPlaying || scheduledDay != day)
        {
            scheduledDay = day;
            ScheduleNext(day);
        }

        wasPlaying = true;
        visuals.RefreshIfNeeded();
        if (Keyboard.current != null &&
            Keyboard.current.f11Key.wasPressedThisFrame)
        {
            forcedGlitchPending = true;
        }

        if (glitchRoutine == null &&
            (forcedGlitchPending || Time.unscaledTime >= nextGlitchTime))
        {
            forcedGlitchPending = false;
            glitchRoutine = StartCoroutine(PlayGlitch());
            ScheduleNext(day);
        }
    }

    private void OnDisable()
    {
        StopActiveGlitch();
        wasPlaying = false;
    }

    private void StopActiveGlitch()
    {
        if (glitchRoutine != null)
            StopCoroutine(glitchRoutine);

        glitchRoutine = null;
        forcedGlitchPending = false;
        visuals?.Reset();
    }

    private static Vector2 GetInterval(int day)
    {
        switch (day)
        {
            case 6:
                return new Vector2(4f, 6f);
            case 7:
                return new Vector2(3f, 4f);
            case 8:
                return new Vector2(2f, 3f);
            case 9:
                return new Vector2(1f, 2f);
            default:
                return Vector2.zero;
        }
    }

    private void ScheduleNext(int day)
    {
        Vector2 interval = GetInterval(day);
        nextGlitchTime = Time.unscaledTime +
            Random.Range(interval.x, interval.y);
    }

    private void ResetSchedule(int day)
    {
        scheduledDay = day;
        nextGlitchTime = float.PositiveInfinity;
    }

    private IEnumerator PlayGlitch()
    {
        UiGlitchFrame frame = visuals.BeginFrame();
        yield return null;
        visuals.EndFrame(frame);
        yield return new WaitForSecondsRealtime(Random.Range(0.08f, 0.14f));
        visuals.EndGhost(frame);
        glitchRoutine = null;
    }
}

public static class UiGlitchTextGhostFactory
{
    public static GameObject Create(
        TextMeshProUGUI source,
        float screenOffset)
    {
        GameObject ghostObject = new GameObject(
            source.name + "GlitchGhost",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement));
        ghostObject.transform.SetParent(source.transform.parent, false);
        ghostObject.GetComponent<LayoutElement>().ignoreLayout = true;

        RectTransform sourceRect = source.rectTransform;
        RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
        ghostRect.anchorMin = sourceRect.anchorMin;
        ghostRect.anchorMax = sourceRect.anchorMax;
        ghostRect.pivot = sourceRect.pivot;
        ghostRect.sizeDelta = sourceRect.sizeDelta;
        ghostRect.anchoredPosition = sourceRect.anchoredPosition +
            Vector2.right * UiGlitchVisuals.ScreenPixelsToLocal(
                sourceRect,
                screenOffset);
        ghostRect.localRotation = sourceRect.localRotation;
        ghostRect.localScale = sourceRect.localScale;
        ghostRect.SetSiblingIndex(sourceRect.GetSiblingIndex() + 1);

        TextMeshProUGUI ghost = ghostObject.GetComponent<TextMeshProUGUI>();
        ghost.text = source.text;
        ghost.font = source.font;
        ghost.fontSharedMaterial = source.fontSharedMaterial;
        ghost.fontSize = source.fontSize;
        ghost.fontStyle = source.fontStyle;
        ghost.enableAutoSizing = source.enableAutoSizing;
        ghost.fontSizeMin = source.fontSizeMin;
        ghost.fontSizeMax = source.fontSizeMax;
        ghost.alignment = source.alignment;
        ghost.overflowMode = source.overflowMode;
        ghost.textWrappingMode = source.textWrappingMode;
        ghost.margin = source.margin;
        ghost.characterSpacing = source.characterSpacing;
        ghost.wordSpacing = source.wordSpacing;
        ghost.lineSpacing = source.lineSpacing;
        ghost.lineSpacingAdjustment = source.lineSpacingAdjustment;
        ghost.paragraphSpacing = source.paragraphSpacing;
        ghost.isRightToLeftText = source.isRightToLeftText;
        ghost.enableVertexGradient = source.enableVertexGradient;
        ghost.colorGradient = source.colorGradient;
        ghost.colorGradientPreset = source.colorGradientPreset;
        ghost.richText = source.richText;
        ghost.maskable = source.maskable;
        ghost.color = new Color(
            source.color.r,
            source.color.g,
            source.color.b,
            source.color.a * 0.65f);
        ghost.raycastTarget = false;
        return ghostObject;
    }
}
