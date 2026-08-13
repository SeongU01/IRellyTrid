using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button), typeof(CanvasGroup))]
public sealed class ButtonInteractionEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    private const float HoverScale = 1.04f;
    private const float PressedScale = 0.96f;
    private const float ScaleResponse = 18f;
    private const float ExitDuration = 0.18f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Button button;
    private TMP_Text label;
    private Vector3 baseScale;
    private Color baseLabelColor;
    private bool hovered;
    private bool pressed;
    private bool selected;
    private bool interactableBeforeExit;
    private bool exitDisabledButton;
    private Coroutine exitRoutine;

    public static ButtonInteractionEffect Attach(Button target)
    {
        if (target == null)
            return null;

        ButtonInteractionEffect effect =
            target.GetComponent<ButtonInteractionEffect>();
        return effect != null
            ? effect
            : target.gameObject.AddComponent<ButtonInteractionEffect>();
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>(true);
        baseScale = rectTransform.localScale;
        baseLabelColor = label != null ? label.color : Color.white;
    }

    private void OnEnable()
    {
        ResetVisuals();
    }

    private void OnDisable()
    {
        if (exitRoutine != null)
        {
            StopCoroutine(exitRoutine);
            exitRoutine = null;
        }

        ResetVisuals();
    }

    private void Update()
    {
        if (exitRoutine != null)
            return;

        float targetMultiplier = pressed
            ? PressedScale
            : hovered || selected
                ? HoverScale
                : 1f;
        float blend = 1f - Mathf.Exp(-ScaleResponse * Time.unscaledDeltaTime);
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            baseScale * targetMultiplier,
            blend);
        UpdateLabelColor(blend);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
        {
            GameAudioManager.PlayUiButton();
            pressed = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
    }

    public void PlayExit(Action onComplete)
    {
        if (exitRoutine != null)
            return;

        exitRoutine = StartCoroutine(ExitRoutine(onComplete));
    }

    private IEnumerator ExitRoutine(Action onComplete)
    {
        interactableBeforeExit = button.interactable;
        exitDisabledButton = true;
        button.interactable = false;
        Vector3 startScale = rectTransform.localScale;
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < ExitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / ExitDuration);
            rectTransform.localScale = Vector3.Lerp(
                startScale,
                baseScale * PressedScale,
                progress);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            yield return null;
        }

        exitRoutine = null;
        onComplete?.Invoke();
    }

    private void UpdateLabelColor(float blend)
    {
        if (label == null)
            return;

        Color targetColor = hovered || selected
            ? Color.Lerp(baseLabelColor, Color.white, 0.3f)
            : baseLabelColor;
        label.color = Color.Lerp(label.color, targetColor, blend);
    }

    private void ResetVisuals()
    {
        hovered = false;
        pressed = false;
        selected = false;

        if (rectTransform != null)
            rectTransform.localScale = baseScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (button != null && exitDisabledButton)
        {
            button.interactable = interactableBeforeExit;
            exitDisabledButton = false;
        }

        if (label != null)
            label.color = baseLabelColor;
    }
}
