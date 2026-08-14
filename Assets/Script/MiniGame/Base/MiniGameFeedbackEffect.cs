using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MiniGameFeedbackEffect : MonoBehaviour
{
    private const float CorrectDuration = 0.2f;
    private const float WrongDuration = 0.22f;
    private const float FlashPeakAlpha = 0.22f;
    private const float CorrectScale = 1.03f;
    private const float WrongShakeDistance = 14f;
    private const float ClearFadeInDuration = 0.25f;
    private const float ClearHoldDuration = 0.3f;
    private const float ClearFadeOutDuration = 0.3f;
    private const float ClearPeakScale = 1.15f;
    private const float ClearFlashAlpha = 0.18f;
    private const float FailureFlashAlpha = 0.2f;
    private static readonly Vector2 ComboBasePosition =
        new Vector2(270f, 220f);

    private RectTransform feedbackTarget;
    private CanvasGroup feedbackCanvasGroup;
    private Vector3 initialScale;
    private Vector2 initialPosition;
    private Image flashImage;
    private TMP_Text comboText;
    private TMP_Text comboGhostText;
    private TMP_Text clearText;
    private Coroutine feedbackRoutine;
    private Coroutine comboRoutine;
    private int comboCount;
    private int currentDay = 1;

    public void SetDay(int day)
    {
        currentDay = Mathf.Max(1, day);
    }

    private void Awake()
    {
        Canvas gameCanvas = GetComponentInChildren<Canvas>(true);
        RectTransform canvasRoot = gameCanvas != null
            ? gameCanvas.transform as RectTransform
            : null;

        if (canvasRoot == null)
        {
            enabled = false;
            return;
        }

        feedbackTarget = CreateMotionRoot(canvasRoot);
        feedbackCanvasGroup = feedbackTarget.gameObject.AddComponent<CanvasGroup>();
        initialScale = feedbackTarget.localScale;
        initialPosition = feedbackTarget.anchoredPosition;
        CreateVisuals(canvasRoot);
    }

    public float PlayCorrect()
    {
        if (!enabled)
            return 0f;

        comboCount++;
        RestartFeedback(CorrectFeedback());
        ShowCombo();
        return CorrectDuration;
    }

    public float PlayWrong()
    {
        if (!enabled)
            return 0f;

        comboCount = 0;

        if (comboRoutine != null)
        {
            StopCoroutine(comboRoutine);
            comboRoutine = null;
        }

        HideCombo();
        RestartFeedback(WrongFeedback());
        return WrongDuration;
    }

    public void ResetFeedback()
    {
        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        if (comboRoutine != null)
            StopCoroutine(comboRoutine);

        feedbackRoutine = null;
        comboRoutine = null;
        comboCount = 0;
        RestoreTarget();

        if (flashImage != null)
            flashImage.color = Color.clear;

        HideCombo();
    }

    public void InvokeAfter(float delay, Action callback)
    {
        StartCoroutine(InvokeAfterRoutine(delay, callback));
    }

    public void PlayClear(Action callback)
    {
        PlayResult(
            "CLEAR",
            new Color(0.45f, 1f, 0.35f),
            ClearFlashAlpha,
            callback);
    }

    public void PlayFailure(bool timedOut, Action callback)
    {
        PlayResult(
            timedOut ? "TIME OVER" : "FAIL",
            new Color(1f, 0.08f, 0.08f),
            FailureFlashAlpha,
            callback);
    }

    private void PlayResult(
        string message,
        Color flashColor,
        float flashAlpha,
        Action callback)
    {
        if (!enabled)
        {
            callback?.Invoke();
            return;
        }

        ResetFeedback();
        StartCoroutine(ResultFeedback(
            message,
            flashColor,
            flashAlpha,
            callback));
    }

    private void OnDisable()
    {
        ResetFeedback();
    }

    private static RectTransform CreateMotionRoot(RectTransform canvasRoot)
    {
        int childCount = canvasRoot.childCount;
        Transform[] existingChildren = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
            existingChildren[i] = canvasRoot.GetChild(i);

        GameObject motionObject = new GameObject(
            "FeedbackMotionRoot",
            typeof(RectTransform));
        RectTransform motionRoot =
            motionObject.GetComponent<RectTransform>();
        motionRoot.SetParent(canvasRoot, false);
        motionRoot.anchorMin = Vector2.zero;
        motionRoot.anchorMax = Vector2.one;
        motionRoot.pivot = new Vector2(0.5f, 0.5f);
        motionRoot.offsetMin = Vector2.zero;
        motionRoot.offsetMax = Vector2.zero;

        for (int i = 0; i < existingChildren.Length; i++)
            existingChildren[i].SetParent(motionRoot, true);

        return motionRoot;
    }

    private void CreateVisuals(RectTransform canvasRoot)
    {
        GameObject flashObject = new GameObject(
            "FeedbackFlash",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        flashObject.transform.SetParent(canvasRoot, false);

        RectTransform flashRect = flashObject.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        flashImage = flashObject.GetComponent<Image>();
        flashImage.color = Color.clear;
        flashImage.raycastTarget = false;

        GameObject comboObject = new GameObject(
            "ComboText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        comboObject.transform.SetParent(canvasRoot, false);

        RectTransform comboRect = comboObject.GetComponent<RectTransform>();
        comboRect.anchorMin = new Vector2(0.5f, 0.5f);
        comboRect.anchorMax = new Vector2(0.5f, 0.5f);
        comboRect.pivot = Vector2.one;
        comboRect.anchoredPosition = ComboBasePosition;
        comboRect.sizeDelta = new Vector2(500f, 100f);

        comboText = comboObject.GetComponent<TMP_Text>();
        comboText.alignment = TextAlignmentOptions.TopRight;
        comboText.color = Color.clear;
        comboText.fontSize = 54f;
        comboText.fontStyle = FontStyles.Bold;
        comboText.raycastTarget = false;

        GameObject ghostObject = Instantiate(comboObject, canvasRoot);
        ghostObject.name = "ComboGhostText";
        comboGhostText = ghostObject.GetComponent<TMP_Text>();
        comboGhostText.color = Color.clear;
        comboGhostText.raycastTarget = false;

        GameObject clearObject = Instantiate(comboObject, canvasRoot);
        clearObject.name = "ClearText";
        clearText = clearObject.GetComponent<TMP_Text>();
        clearText.text = "CLEAR";
        clearText.color = Color.clear;
        clearText.fontSize = 72f;
        clearText.alignment = TextAlignmentOptions.Center;
        clearText.raycastTarget = false;
        clearText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        clearText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        clearText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        clearText.rectTransform.anchoredPosition = Vector2.zero;
        clearText.rectTransform.sizeDelta = new Vector2(600f, 140f);

        TMP_Text[] sourceTexts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < sourceTexts.Length; i++)
        {
            if (sourceTexts[i] == comboText ||
                sourceTexts[i] == comboGhostText ||
                sourceTexts[i] == clearText)
                continue;

            comboText.font = sourceTexts[i].font;
            comboGhostText.font = sourceTexts[i].font;
            clearText.font = sourceTexts[i].font;
            break;
        }

        flashObject.transform.SetAsLastSibling();
        ghostObject.transform.SetAsLastSibling();
        comboObject.transform.SetAsLastSibling();
        clearObject.transform.SetAsLastSibling();
    }

    private IEnumerator ResultFeedback(
        string message,
        Color flashColor,
        float flashAlpha,
        Action callback)
    {
        feedbackCanvasGroup.blocksRaycasts = false;
        feedbackCanvasGroup.interactable = false;
        clearText.text = message;

        float elapsed = 0f;
        while (elapsed < ClearFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / ClearFadeInDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            clearText.color = new Color(0f, 0f, 0f, eased);
            clearText.rectTransform.localScale = Vector3.one *
                Mathf.Lerp(0.75f, ClearPeakScale, eased);
            flashImage.color = new Color(
                flashColor.r,
                flashColor.g,
                flashColor.b,
                Mathf.Sin(progress * Mathf.PI) * flashAlpha);
            yield return null;
        }

        clearText.color = Color.black;
        clearText.rectTransform.localScale = Vector3.one * ClearPeakScale;
        flashImage.color = Color.clear;
        yield return new WaitForSeconds(ClearHoldDuration);

        elapsed = 0f;
        while (elapsed < ClearFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / ClearFadeOutDuration);
            float alpha = 1f - progress;
            clearText.color = new Color(0f, 0f, 0f, alpha);
            feedbackCanvasGroup.alpha = alpha;
            yield return null;
        }

        clearText.color = Color.clear;
        flashImage.color = Color.clear;
        feedbackCanvasGroup.alpha = 0f;
        callback?.Invoke();
    }

    private void RestartFeedback(IEnumerator routine)
    {
        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        RestoreTarget();
        flashImage.color = Color.clear;
        feedbackRoutine = StartCoroutine(routine);
    }

    private IEnumerator CorrectFeedback()
    {
        float elapsed = 0f;

        while (elapsed < CorrectDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / CorrectDuration);
            float pulse = Mathf.Sin(progress * Mathf.PI);
            feedbackTarget.localScale = Vector3.LerpUnclamped(
                initialScale,
                initialScale * CorrectScale,
                pulse);
            yield return null;
        }

        RestoreTarget();
        flashImage.color = Color.clear;
        feedbackRoutine = null;
    }

    private IEnumerator WrongFeedback()
    {
        Color flashColor = new Color(1f, 0.08f, 0.08f, FlashPeakAlpha);
        float elapsed = 0f;

        while (elapsed < WrongDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / WrongDuration);
            float strength = 1f - progress;
            float offset = Mathf.Sin(progress * Mathf.PI * 8f) *
                WrongShakeDistance * strength;
            feedbackTarget.anchoredPosition = initialPosition +
                Vector2.right * offset;
            flashImage.color = Color.LerpUnclamped(
                flashColor,
                Color.clear,
                progress);
            yield return null;
        }

        RestoreTarget();
        flashImage.color = Color.clear;
        feedbackRoutine = null;
    }

    private void ShowCombo()
    {
        if (comboCount < 2 || comboText == null)
            return;

        if (comboRoutine != null)
            StopCoroutine(comboRoutine);

        comboText.text = $"COMBO {comboCount}";
        comboGhostText.text = comboText.text;
        comboRoutine = StartCoroutine(ComboFeedback());
    }

    private IEnumerator ComboFeedback()
    {
        const float duration = 0.45f;
        float elapsed = 0f;
        float targetScale = Mathf.Min(1.35f, 1f + (comboCount - 2) * 0.06f);
        Color visibleColor = new Color(0f, 0f, 0f, 0.92f);
        bool isGlitched = currentDay >= 6 && currentDay <= 9;
        int glitchLevel = isGlitched ? currentDay - 5 : 0;
        RectTransform comboRect = comboText.rectTransform;
        RectTransform ghostRect = comboGhostText.rectTransform;
        Vector2 basePosition = ComboBasePosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float alpha = 1f - Mathf.Clamp01((progress - 0.55f) / 0.45f);
            float pop = 1f + Mathf.Sin(progress * Mathf.PI) * 0.18f;
            comboText.rectTransform.localScale = Vector3.one *
                targetScale * pop;
            float jitterX = isGlitched
                ? Mathf.Sin(elapsed * (45f + glitchLevel * 11f)) *
                    glitchLevel * 1.8f
                : 0f;
            float jitterY = glitchLevel >= 3
                ? Mathf.Cos(elapsed * 67f) * (glitchLevel - 2) * 1.4f
                : 0f;
            float rotation = glitchLevel >= 2
                ? Mathf.Sin(elapsed * 39f) * glitchLevel * 1.4f
                : 0f;
            comboRect.anchoredPosition = basePosition +
                new Vector2(jitterX, jitterY);
            comboRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            comboText.characterSpacing = 0f;
            comboText.color = new Color(
                visibleColor.r,
                visibleColor.g,
                visibleColor.b,
                visibleColor.a * alpha);
            if (glitchLevel >= 3)
            {
                ApplyIrregularCharacterOffsets(
                    comboText,
                    elapsed,
                    glitchLevel);
            }

            bool showGhost = glitchLevel >= 4 &&
                Mathf.Sin(elapsed * 82f) > 0.35f;
            ghostRect.anchoredPosition = comboRect.anchoredPosition +
                new Vector2(-8f, 4f);
            ghostRect.localRotation = Quaternion.Euler(
                0f,
                0f,
                -rotation * 0.65f);
            ghostRect.localScale = comboRect.localScale * 1.03f;
            comboGhostText.characterSpacing = 0f;
            comboGhostText.color = showGhost
                ? new Color(0f, 0f, 0f, 0.38f * alpha)
                : Color.clear;
            yield return null;
        }

        HideCombo();
        comboRoutine = null;
    }

    private static IEnumerator InvokeAfterRoutine(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    private static void ApplyIrregularCharacterOffsets(
        TMP_Text text,
        float elapsed,
        int glitchLevel)
    {
        text.ForceMeshUpdate();
        TMP_TextInfo textInfo = text.textInfo;
        float strength = (glitchLevel - 2) * 1.8f;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (!character.isVisible)
                continue;

            int materialIndex = character.materialReferenceIndex;
            int vertexIndex = character.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            float offset = Mathf.Sin(
                elapsed * (47f + i * 3f) + i * 2.17f) *
                strength;
            Vector3 translation = Vector3.right * offset;

            vertices[vertexIndex] += translation;
            vertices[vertexIndex + 1] += translation;
            vertices[vertexIndex + 2] += translation;
            vertices[vertexIndex + 3] += translation;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            text.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    private void RestoreTarget()
    {
        if (feedbackTarget == null)
            return;

        feedbackTarget.localScale = initialScale;
        feedbackTarget.anchoredPosition = initialPosition;
    }

    private void HideCombo()
    {
        if (comboText == null)
            return;

        comboText.color = Color.clear;
        comboText.rectTransform.localScale = Vector3.one;
        comboText.rectTransform.anchoredPosition = ComboBasePosition;
        comboText.rectTransform.localRotation = Quaternion.identity;
        comboText.characterSpacing = 0f;

        if (clearText != null)
        {
            clearText.color = Color.clear;
            clearText.rectTransform.localScale = Vector3.one;
        }

        if (comboGhostText == null)
            return;

        comboGhostText.color = Color.clear;
        comboGhostText.rectTransform.localScale = Vector3.one;
        comboGhostText.rectTransform.anchoredPosition =
            ComboBasePosition;
        comboGhostText.rectTransform.localRotation = Quaternion.identity;
        comboGhostText.characterSpacing = 0f;
    }
}
