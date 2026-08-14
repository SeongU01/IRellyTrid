using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FatigueScreenEffect : MonoBehaviour
{
    private const int OverlaySortingOrder = 950;
    private const float MaximumVerticalOffset = 12f;
    private const float VignetteMaximumAlpha = 0.82f;
    private const float HighFatigueThreshold = 0.8f;

    private readonly List<RectTransform> motionRoots =
        new List<RectTransform>();
    private MiniGameManager miniGameManager;
    private PlayerStatus playerStatus;
    private Camera mainCamera;
    private Vector3 initialCameraPosition;
    private Image vignetteImage;
    private Image hazeImage;
    private Image blackoutImage;
    private TMP_Text healthLossText;
    private Texture2D vignetteTexture;
    private Sprite vignetteSprite;
    private float fatigueRatio;
    private float nextCanvasScanTime;
    private float nextHazeTime;
    private float hazePulseEndsAt;
    private Coroutine maximumRoutine;
    private bool isMaximumEffectPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddToMiniGameManager()
    {
        MiniGameManager manager = FindAnyObjectByType<MiniGameManager>();
        if (manager != null &&
            manager.GetComponent<FatigueScreenEffect>() == null)
        {
            manager.gameObject.AddComponent<FatigueScreenEffect>();
        }
    }

    private void Awake()
    {
        miniGameManager = GetComponent<MiniGameManager>();
        mainCamera = Camera.main;
        if (mainCamera != null)
            initialCameraPosition = mainCamera.transform.localPosition;

        CreateOverlay();
    }

    private void OnEnable()
    {
        playerStatus = PlayerStatus.Instance;
        if (playerStatus == null)
            return;

        playerStatus.FatigueChanged += HandleFatigueChanged;
        playerStatus.FatigueMaximumReached += HandleFatigueMaximumReached;
        HandleFatigueChanged(playerStatus.Fatigue, playerStatus.MaxFatigue);
    }

    private void OnDisable()
    {
        if (playerStatus != null)
        {
            playerStatus.FatigueChanged -= HandleFatigueChanged;
            playerStatus.FatigueMaximumReached -=
                HandleFatigueMaximumReached;
        }

        ApplyVerticalOffset(0f);
        if (maximumRoutine != null)
            StopCoroutine(maximumRoutine);

        maximumRoutine = null;
        isMaximumEffectPlaying = false;
        if (hazeImage != null)
            hazeImage.color = Color.clear;
        if (blackoutImage != null)
            blackoutImage.color = Color.clear;
        if (healthLossText != null)
            healthLossText.color = Color.clear;
    }

    private void OnDestroy()
    {
        if (vignetteSprite != null)
            Destroy(vignetteSprite);

        if (vignetteTexture != null)
            Destroy(vignetteTexture);
    }

    private void Update()
    {
        bool isPlaying = miniGameManager != null &&
            miniGameManager.IsMiniGamePlaying;
        float activeRatio = isPlaying ? fatigueRatio : 0f;

        if (Time.unscaledTime >= nextCanvasScanTime)
        {
            nextCanvasScanTime = Time.unscaledTime + 0.5f;
            RefreshMotionRoots();
        }

        UpdateVignette(activeRatio);
        if (!isMaximumEffectPlaying)
            UpdateScreenMotion(activeRatio);
        UpdateHaze(activeRatio);
    }

    private void HandleFatigueChanged(float current, float maximum)
    {
        fatigueRatio = maximum > 0f
            ? Mathf.Clamp01(current / maximum)
            : 0f;
    }

    private void HandleFatigueMaximumReached()
    {
        if (maximumRoutine != null)
            StopCoroutine(maximumRoutine);

        maximumRoutine = StartCoroutine(PlayMaximumEffect());
    }

    private void UpdateVignette(float ratio)
    {
        float intensity = Mathf.InverseLerp(0.3f, 1f, ratio);
        float highFatiguePulse = ratio >= HighFatigueThreshold
            ? 0.88f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.12f
            : 1f;
        vignetteImage.color = new Color(
            0f,
            0f,
            0f,
            intensity * VignetteMaximumAlpha * highFatiguePulse);
    }

    private void UpdateScreenMotion(float ratio)
    {
        float intensity = Mathf.InverseLerp(0.6f, 1f, ratio);
        float offset = Mathf.Sin(Time.unscaledTime * 0.65f) *
            MaximumVerticalOffset * intensity;
        float rotation = Mathf.Sin(Time.unscaledTime * 0.42f) *
            intensity * 0.45f;
        float breathingScale = ratio >= HighFatigueThreshold
            ? 1f + Mathf.Sin(Time.unscaledTime * 1.25f) *
                Mathf.InverseLerp(HighFatigueThreshold, 1f, ratio) * 0.012f
            : 1f;
        ApplyScreenMotion(offset, rotation, breathingScale);
    }

    private void ApplyVerticalOffset(float offset)
    {
        ApplyScreenMotion(offset, 0f, 1f);
    }

    private void ApplyScreenMotion(
        float verticalOffset,
        float rotation,
        float scale)
    {
        for (int i = motionRoots.Count - 1; i >= 0; i--)
        {
            RectTransform root = motionRoots[i];
            if (root == null)
            {
                motionRoots.RemoveAt(i);
                continue;
            }

            root.anchoredPosition = Vector2.up * verticalOffset;
            root.localRotation = Quaternion.Euler(0f, 0f, rotation);
            root.localScale = Vector3.one * scale;
        }

        if (mainCamera != null)
        {
            mainCamera.transform.localPosition = initialCameraPosition +
                Vector3.up * (verticalOffset * 0.01f);
        }
    }

    private void UpdateHaze(float ratio)
    {
        if (isMaximumEffectPlaying)
            return;

        if (ratio < HighFatigueThreshold)
        {
            hazeImage.color = Color.clear;
            return;
        }

        if (Time.unscaledTime >= nextHazeTime)
        {
            hazePulseEndsAt = Time.unscaledTime + 0.18f;
            nextHazeTime = hazePulseEndsAt + Random.Range(1.8f, 3.5f);
        }

        float pulseProgress = Mathf.InverseLerp(
            hazePulseEndsAt - 0.18f,
            hazePulseEndsAt,
            Time.unscaledTime);
        float pulse = Time.unscaledTime < hazePulseEndsAt
            ? Mathf.Sin(pulseProgress * Mathf.PI)
            : 0f;
        float strength = Mathf.InverseLerp(HighFatigueThreshold, 1f, ratio);
        hazeImage.color = new Color(0f, 0f, 0f,
            pulse * strength * 0.22f);

        if (pulse > 0f)
        {
            float currentOffset = Mathf.Sin(Time.unscaledTime * 0.65f) *
                MaximumVerticalOffset * strength;
            ApplyScreenMotion(
                currentOffset - pulse * 5f * strength,
                Mathf.Sin(Time.unscaledTime * 0.42f) * strength * 0.45f,
                1f + pulse * 0.009f * strength);
        }
    }

    private void RefreshMotionRoots()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Exclude);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay ||
                canvas.sortingOrder >= OverlaySortingOrder ||
                canvas.transform.Find("FatigueMotionRoot") != null)
            {
                continue;
            }

            WrapCanvas(canvas.transform as RectTransform);
        }
    }

    private void WrapCanvas(RectTransform canvasRoot)
    {
        if (canvasRoot == null)
            return;

        int childCount = canvasRoot.childCount;
        Transform[] children = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
            children[i] = canvasRoot.GetChild(i);

        GameObject rootObject = new GameObject(
            "FatigueMotionRoot",
            typeof(RectTransform));
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.SetParent(canvasRoot, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        for (int i = 0; i < children.Length; i++)
            children[i].SetParent(root, true);

        motionRoots.Add(root);
    }

    private void CreateOverlay()
    {
        GameObject canvasObject = new GameObject(
            "FatigueEffectCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortingOrder;

        vignetteImage = CreateFullscreenImage(
            "FatigueVignette",
            canvasObject.transform);
        vignetteSprite = CreateVignetteSprite(out vignetteTexture);
        vignetteImage.sprite = vignetteSprite;
        vignetteImage.type = Image.Type.Simple;
        vignetteImage.preserveAspect = false;
        hazeImage = CreateFullscreenImage("FatigueHaze", canvasObject.transform);
        blackoutImage = CreateFullscreenImage(
            "FatigueBlackout",
            canvasObject.transform);

        GameObject textObject = new GameObject(
            "HealthLossText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(600f, 120f);
        healthLossText = textObject.GetComponent<TMP_Text>();
        healthLossText.text = "HEALTH -1";
        healthLossText.fontSize = 64f;
        healthLossText.alignment = TextAlignmentOptions.Center;
        healthLossText.color = Color.clear;
        healthLossText.raycastTarget = false;

        TMP_Text source = FindAnyObjectByType<TMP_Text>();
        if (source != null)
            healthLossText.font = source.font;
    }

    private static Image CreateFullscreenImage(string name, Transform parent)
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = imageObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite CreateVignetteSprite(out Texture2D texture)
    {
        const int size = 64;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                float ny = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                float edge = Mathf.SmoothStep(0.42f, 1f, Mathf.Max(nx, ny));
                pixels[y * size + x] = new Color(1f, 1f, 1f, edge);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f));
    }

    private IEnumerator PlayMaximumEffect()
    {
        isMaximumEffectPlaying = true;
        float elapsed = 0f;
        const float fadeInDuration = 0.12f;
        const float fadeOutDuration = 0.22f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            ApplyScreenMotion(-18f * alpha, 0f, 1f - alpha * 0.025f);
            blackoutImage.color = new Color(0f, 0f, 0f, alpha * 0.96f);
            hazeImage.color = new Color(0.75f, 0f, 0f, alpha * 0.28f);
            healthLossText.color = new Color(1f, 1f, 1f, alpha);
            healthLossText.rectTransform.localScale = Vector3.one *
                Mathf.Lerp(0.65f, 1.25f, 1f - Mathf.Pow(1f - alpha, 3f));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
            float alpha = 1f - progress;
            ApplyScreenMotion(Mathf.Lerp(-18f, 0f, progress), 0f,
                Mathf.Lerp(0.975f, 1f, progress));
            blackoutImage.color = new Color(0f, 0f, 0f, alpha * 0.96f);
            hazeImage.color = new Color(0.75f, 0f, 0f, alpha * 0.28f);
            healthLossText.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        blackoutImage.color = Color.clear;
        hazeImage.color = Color.clear;
        healthLossText.color = Color.clear;
        healthLossText.rectTransform.localScale = Vector3.one;
        ApplyScreenMotion(0f, 0f, 1f);
        isMaximumEffectPlaying = false;
        maximumRoutine = null;
    }
}
