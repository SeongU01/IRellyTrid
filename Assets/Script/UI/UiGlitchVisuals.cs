using System.Collections.Generic;
using TMPro;
using UnityEngine;

public readonly struct UiGlitchFrame
{
    public UiGlitchFrame(
        RectTransform source,
        Vector2 sourcePosition,
        GameObject ghost,
        GameObject secondGhost)
    {
        Source = source;
        SourcePosition = sourcePosition;
        Ghost = ghost;
        SecondGhost = secondGhost;
    }

    public RectTransform Source { get; }
    public Vector2 SourcePosition { get; }
    public GameObject Ghost { get; }
    public GameObject SecondGhost { get; }
}

public sealed class UiGlitchVisuals
{
    private const int ExcludedSortingOrder = 900;
    private const float CanvasScanInterval = 0.5f;

    private readonly List<RectTransform> motionRoots =
        new List<RectTransform>();
    private readonly List<TextMeshProUGUI> textCandidates =
        new List<TextMeshProUGUI>();
    private readonly List<GameObject> activeGhosts =
        new List<GameObject>();
    private float nextCanvasScanTime;
    private UiGlitchFrame activeFrame;
    private bool hasActiveFrame;

    public void RefreshIfNeeded()
    {
        if (Time.unscaledTime < nextCanvasScanTime)
            return;

        nextCanvasScanTime = Time.unscaledTime + CanvasScanInterval;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Exclude);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (!canvas.isRootCanvas ||
                canvas.renderMode != RenderMode.ScreenSpaceOverlay ||
                canvas.sortingOrder >= ExcludedSortingOrder ||
                FindMotionRoot(canvas.transform) != null)
            {
                continue;
            }

            RectTransform fatigueRoot =
                canvas.transform.Find("FatigueMotionRoot") as RectTransform;
            CreateMotionRoot(fatigueRoot != null
                ? fatigueRoot
                : canvas.transform as RectTransform);
        }
    }

    public UiGlitchFrame BeginFrame()
    {
        RemoveMissingRoots();
        TextMeshProUGUI source = FindTextCandidate();
        RectTransform sourceRect = source != null
            ? source.rectTransform
            : null;
        Vector2 sourcePosition = sourceRect != null
            ? sourceRect.anchoredPosition
            : Vector2.zero;
        int direction = Random.value < 0.5f ? -1 : 1;
        float canvasOffsetPixels = direction * Random.Range(2, 5);

        for (int i = 0; i < motionRoots.Count; i++)
        {
            float offset = ScreenPixelsToLocal(
                motionRoots[i],
                canvasOffsetPixels);
            motionRoots[i].anchoredPosition = Vector2.right * offset;
        }

        GameObject ghost = source != null
            ? UiGlitchTextGhostFactory.Create(
                source,
                direction * Random.Range(1, 3))
            : null;
        GameObject secondGhost = source != null
            ? UiGlitchTextGhostFactory.Create(
                source,
                -direction * Random.Range(1, 3))
            : null;
        if (ghost != null)
            activeGhosts.Add(ghost);
        if (secondGhost != null)
            activeGhosts.Add(secondGhost);
        if (sourceRect != null)
        {
            float sourceOffset = ScreenPixelsToLocal(sourceRect, 2f);
            sourceRect.anchoredPosition +=
                Vector2.right * direction * sourceOffset;
        }

        activeFrame = new UiGlitchFrame(
            sourceRect,
            sourcePosition,
            ghost,
            secondGhost);
        hasActiveFrame = true;
        return activeFrame;
    }

    public void EndFrame(UiGlitchFrame frame)
    {
        RestoreMotionRoots();
        if (frame.Source != null)
            frame.Source.anchoredPosition = frame.SourcePosition;

        hasActiveFrame = false;
    }

    public void EndGhost(UiGlitchFrame frame)
    {
        DestroyGhost(frame.Ghost);
        DestroyGhost(frame.SecondGhost);
    }

    private void DestroyGhost(GameObject ghost)
    {
        if (ghost == null)
            return;

        activeGhosts.Remove(ghost);
        Object.Destroy(ghost);
    }

    public void Reset()
    {
        RestoreMotionRoots();
        if (hasActiveFrame && activeFrame.Source != null)
            activeFrame.Source.anchoredPosition = activeFrame.SourcePosition;

        hasActiveFrame = false;
        for (int i = 0; i < activeGhosts.Count; i++)
        {
            if (activeGhosts[i] != null)
                Object.Destroy(activeGhosts[i]);
        }

        activeGhosts.Clear();
    }

    private void CreateMotionRoot(RectTransform parent)
    {
        if (parent == null)
            return;

        int childCount = parent.childCount;
        Transform[] children = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
            children[i] = parent.GetChild(i);

        GameObject rootObject = new GameObject(
            "UiGlitchMotionRoot",
            typeof(RectTransform));
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        for (int i = 0; i < children.Length; i++)
            children[i].SetParent(root, true);

        motionRoots.Add(root);
    }

    private TextMeshProUGUI FindTextCandidate()
    {
        textCandidates.Clear();
        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Exclude);

        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];
            if (!text.name.EndsWith("GlitchGhost") &&
                !string.IsNullOrWhiteSpace(text.text) &&
                text.color.a > 0.05f &&
                IsInsideMotionRoot(text.transform))
            {
                textCandidates.Add(text);
            }
        }

        return textCandidates.Count > 0
            ? textCandidates[Random.Range(0, textCandidates.Count)]
            : null;
    }

    public static float ScreenPixelsToLocal(
        RectTransform target,
        float screenPixels)
    {
        Canvas canvas = target.GetComponentInParent<Canvas>();
        float canvasScale = canvas != null ? canvas.scaleFactor : 1f;
        float parentScale = target.parent != null
            ? Mathf.Abs(target.parent.lossyScale.x)
            : 1f;
        return screenPixels /
            Mathf.Max(0.01f, canvasScale * parentScale);
    }

    private bool IsInsideMotionRoot(Transform target)
    {
        for (int i = 0; i < motionRoots.Count; i++)
        {
            if (motionRoots[i] != null && target.IsChildOf(motionRoots[i]))
                return true;
        }

        return false;
    }

    private static RectTransform FindMotionRoot(Transform canvas)
    {
        RectTransform[] rects =
            canvas.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i].name == "UiGlitchMotionRoot")
                return rects[i];
        }

        return null;
    }

    private void RestoreMotionRoots()
    {
        RemoveMissingRoots();
        for (int i = 0; i < motionRoots.Count; i++)
            motionRoots[i].anchoredPosition = Vector2.zero;
    }

    private void RemoveMissingRoots()
    {
        for (int i = motionRoots.Count - 1; i >= 0; i--)
        {
            if (motionRoots[i] == null)
                motionRoots.RemoveAt(i);
        }
    }
}
