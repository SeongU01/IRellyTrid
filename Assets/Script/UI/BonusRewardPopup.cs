using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BonusRewardPopup : MonoBehaviour
{
    private RectTransform popupRect;
    private CanvasGroup canvasGroup;
    private TMP_Text rewardText;
    private Coroutine animationRoutine;

    public static BonusRewardPopup Create(
        Transform parent,
        TMP_FontAsset fontAsset)
    {
        GameObject popupObject = new GameObject(
            "BonusRewardPopup",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(BonusRewardPopup));
        popupObject.transform.SetParent(parent, false);

        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = new Vector2(-165f, -150f);
        popupRect.sizeDelta = new Vector2(600f, 110f);

        Image image = popupObject.GetComponent<Image>();
        image.color = new Color(0.72f, 0.95f, 0.48f, 0.92f);
        image.raycastTarget = false;

        GameObject textObject = new GameObject(
            "RewardText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(popupObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 8f);
        textRect.offsetMax = new Vector2(-20f, -8f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = fontAsset;
        text.fontSize = 42f;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        BonusRewardPopup popup = popupObject.GetComponent<BonusRewardPopup>();
        popup.Initialize(text);
        popupObject.SetActive(false);
        return popup;
    }

    private void Initialize(TMP_Text text)
    {
        popupRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rewardText = text;
    }

    public void Show(float multiplier)
    {
        if (rewardText == null)
            return;

        rewardText.text = $"다음 날 공부량 ×{multiplier:0.0}";
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        canvasGroup.alpha = 0f;
        popupRect.localScale = Vector3.one * 0.8f;
        yield return AnimateTo(1f, 1.08f, 0.18f);
        yield return AnimateTo(1f, 1f, 0.12f);
        yield return new WaitForSeconds(0.65f);
        yield return AnimateTo(0f, 1f, 0.2f);
        animationRoutine = null;
        gameObject.SetActive(false);
    }

    private IEnumerator AnimateTo(float alpha, float scale, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = popupRect.localScale;
        Vector3 targetScale = Vector3.one * scale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, alpha, eased);
            popupRect.localScale = Vector3.Lerp(startScale, targetScale, eased);
            yield return null;
        }

        canvasGroup.alpha = alpha;
        popupRect.localScale = targetScale;
    }
}
