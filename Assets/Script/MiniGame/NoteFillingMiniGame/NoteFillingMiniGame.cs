using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class NoteFillingMiniGame : MiniGameBase
{
    private const string Alphabet =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private sealed class PageView
    {
        public RectTransform Root { get; }
        public TMP_Text Text { get; }

        public PageView(RectTransform root, TMP_Text text)
        {
            Root = root;
            Text = text;
        }
    }

    [Header("Game Area")]
    [SerializeField] private Vector2 gameAreaCenter =
        new Vector2(-165f, -150f);
    [SerializeField] private Vector2 gameAreaSize =
        new Vector2(860f, 700f);
    [SerializeField] private Vector2 activePagePosition =
        new Vector2(-210f, 0f);
    [SerializeField] private Vector2 completedPageStackPosition =
        new Vector2(210f, 0f);
    [SerializeField] private Vector2 pageSize =
        new Vector2(360f, 509f);

    [Header("Page Visual")]
    [SerializeField] private Sprite noteSprite;
    [SerializeField] private Color temporaryPageColor =
        Color.white;
    [SerializeField] private Color pageBorderColor =
        new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color pageShadowColor =
        new Color(0f, 0f, 0f, 0.28f);
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField, Min(1f)] private float fontSize = 48f;
    [SerializeField] private float lineSpacing = -12f;
    [SerializeField, Min(0f)] private float textPadding = 24f;

    [Header("Writing")]
    [SerializeField, Min(1)] private int charactersPerPage = 100;
    [SerializeField, Min(1)] private int charactersPerLine = 10;

    [Header("Page Transition")]
    [SerializeField, Min(0.01f)] private float pageSlideDuration = 0.35f;
    [SerializeField] private Vector2 completedPageStackOffset =
        new Vector2(5f, -5f);
    [SerializeField, Min(0)] private int maximumVisibleStackOffset = 8;
    [SerializeField, Min(0f)] private float stackRotationStep = 1.25f;

    private readonly StringBuilder currentCharacters = new();
    private RectTransform pageRoot;
    private PageView currentPage;
    private BonusRewardPopup rewardPopup;
    private int completedPageCount;
    private int totalCharacterCount;

    public int CompletedPageCount => completedPageCount;
    public int CurrentPageCharacterCount => currentCharacters.Length;
    public int TotalCharacterCount => totalCharacterCount;

    protected override void OnStart()
    {
        completedPageCount = 0;
        totalCharacterCount = 0;
        currentCharacters.Clear();
        CreateRuntimeView();
        currentPage = CreatePage();

#if UNITY_EDITOR
        Debug.Log(
            $"[NoteFillingMiniGame] Started. " +
            $"{charactersPerPage} characters complete one page.");
#endif
    }

    private void Update()
    {
        if (!IsPlaying)
            return;

        Mouse mouse = Mouse.current;

        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        WriteRandomCharacter();
    }

    public override void Timeout()
    {
        if (!IsPlaying)
            return;

        CompleteGame();
    }

    protected override void Success()
    {
        if (!IsPlaying)
            return;

        CompleteGame();
    }

    protected override void OnEnd()
    {
        StopAllCoroutines();
    }

    private void WriteRandomCharacter()
    {
        int randomIndex = UnityEngine.Random.Range(0, Alphabet.Length);
        currentCharacters.Append(Alphabet[randomIndex]);
        totalCharacterCount++;
        GameAudioManager.PlayTyping();
        RefreshCurrentPageText();

        if (currentCharacters.Length < charactersPerPage)
            return;

        CompleteCurrentPage();
    }

    private void CompleteCurrentPage()
    {
        PageView completedPage = currentPage;
        completedPageCount++;
        currentCharacters.Clear();

        currentPage = CreatePage();
        StartCoroutine(StackCompletedPage(
            completedPage,
            completedPageCount - 1));

        if (completedPageCount == 2)
            rewardPopup?.Show(1.5f);
        else if (completedPageCount == 3)
            rewardPopup?.Show(2f);

#if UNITY_EDITOR
        Debug.Log(
            $"[NoteFillingMiniGame] Page {completedPageCount} completed. " +
            $"Total characters: {totalCharacterCount}.");
#endif
    }

    private IEnumerator StackCompletedPage(
        PageView completedPage,
        int completedPageIndex)
    {
        if (completedPage?.Root == null)
            yield break;

        Vector2 startPosition = completedPage.Root.anchoredPosition;
        int visibleOffsetIndex = Mathf.Min(
            Mathf.Max(0, completedPageIndex),
            maximumVisibleStackOffset);
        Vector2 targetPosition = completedPageStackPosition +
            completedPageStackOffset * visibleOffsetIndex;
        float targetRotation = GetStackRotation(completedPageIndex);
        float elapsed = 0f;

        while (elapsed < pageSlideDuration &&
               completedPage.Root != null)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / pageSlideDuration);
            float easedProgress = 1f -
                Mathf.Pow(1f - progress, 3f);
            completedPage.Root.anchoredPosition = Vector2.LerpUnclamped(
                startPosition,
                targetPosition,
                easedProgress);
            completedPage.Root.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(0f, targetRotation, easedProgress));
            yield return null;
        }

        if (completedPage.Root != null)
        {
            completedPage.Root.anchoredPosition = targetPosition;
            completedPage.Root.localRotation = Quaternion.Euler(
                0f,
                0f,
                targetRotation);
        }
    }

    private float GetStackRotation(int completedPageIndex)
    {
        int direction = completedPageIndex % 2 == 0 ? -1 : 1;
        return direction * stackRotationStep;
    }

    private void CompleteGame()
    {
        float nextDayStudyMultiplier = GetNextDayStudyMultiplier();

#if UNITY_EDITOR
        Debug.Log(
            $"[NoteFillingMiniGame] Completed pages: " +
            $"{completedPageCount}, current page: " +
            $"{currentCharacters.Length}/{charactersPerPage}, " +
            $"total characters: {totalCharacterCount}. " +
            $"Next-day study multiplier: " +
            $"x{nextDayStudyMultiplier:0.##}.");
#endif

        SuccessWithNextDayStudyMultiplier(nextDayStudyMultiplier);
    }

    private float GetNextDayStudyMultiplier()
    {
        if (completedPageCount >= 3)
            return 2f;

        if (completedPageCount == 2)
            return 1.5f;

        return 1f;
    }

    private void CreateRuntimeView()
    {
        GameObject canvasObject = new GameObject(
            "NoteFillingCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        rewardPopup = BonusRewardPopup.Create(
            canvasObject.transform,
            KoreanFontBootstrap.FontAsset ?? fontAsset);

        GameObject rootObject = new GameObject(
            "PageRoot",
            typeof(RectTransform),
            typeof(RectMask2D));
        rootObject.transform.SetParent(canvasObject.transform, false);

        pageRoot = rootObject.GetComponent<RectTransform>();
        pageRoot.anchorMin = new Vector2(0.5f, 0.5f);
        pageRoot.anchorMax = new Vector2(0.5f, 0.5f);
        pageRoot.pivot = new Vector2(0.5f, 0.5f);
        pageRoot.anchoredPosition = gameAreaCenter;
        pageRoot.sizeDelta = gameAreaSize;
    }

    private PageView CreatePage()
    {
        GameObject pageObject = new GameObject(
            $"Page_{completedPageCount + 1}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        pageObject.transform.SetParent(pageRoot, false);

        RectTransform pageRect =
            pageObject.GetComponent<RectTransform>();
        pageRect.anchorMin = new Vector2(0.5f, 0.5f);
        pageRect.anchorMax = new Vector2(0.5f, 0.5f);
        pageRect.pivot = new Vector2(0.5f, 0.5f);
        pageRect.anchoredPosition = activePagePosition;
        pageRect.sizeDelta = pageSize;

        Image pageImage = pageObject.GetComponent<Image>();
        pageImage.sprite = noteSprite;
        pageImage.color = noteSprite != null
            ? Color.white
            : temporaryPageColor;
        pageImage.preserveAspect = noteSprite != null;
        pageImage.raycastTarget = false;

        Shadow pageShadow = pageObject.AddComponent<Shadow>();
        pageShadow.effectColor = pageShadowColor;
        pageShadow.effectDistance = new Vector2(8f, -8f);
        pageShadow.useGraphicAlpha = true;

        Outline pageOutline = pageObject.AddComponent<Outline>();
        pageOutline.effectColor = pageBorderColor;
        pageOutline.effectDistance = new Vector2(2f, -2f);
        pageOutline.useGraphicAlpha = true;

        GameObject textObject = new GameObject(
            "Characters",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(pageObject.transform, false);

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.one * textPadding;
        textRect.offsetMax = Vector2.one * -textPadding;

        TextMeshProUGUI pageText =
            textObject.GetComponent<TextMeshProUGUI>();

        if (fontAsset != null)
            pageText.font = fontAsset;

        pageText.fontSize = fontSize;
        pageText.color = textColor;
        pageText.alignment = TextAlignmentOptions.TopLeft;
        pageText.textWrappingMode = TextWrappingModes.NoWrap;
        pageText.overflowMode = TextOverflowModes.Masking;
        pageText.lineSpacing = lineSpacing;
        pageText.raycastTarget = false;
        pageText.text = string.Empty;

        return new PageView(pageRect, pageText);
    }

    private void RefreshCurrentPageText()
    {
        if (currentPage?.Text == null)
            return;

        StringBuilder displayText = new StringBuilder(
            currentCharacters.Length +
            currentCharacters.Length / charactersPerLine);

        for (int index = 0; index < currentCharacters.Length; index++)
        {
            displayText.Append(currentCharacters[index]);

            bool isEndOfLine =
                (index + 1) % charactersPerLine == 0;
            bool hasMoreCharacters =
                index + 1 < currentCharacters.Length;

            if (isEndOfLine && hasMoreCharacters)
                displayText.Append('\n');
        }

        currentPage.Text.text = displayText.ToString();
    }

    private void OnValidate()
    {
        gameAreaSize.x = Mathf.Max(1f, gameAreaSize.x);
        gameAreaSize.y = Mathf.Max(1f, gameAreaSize.y);
        pageSize.x = Mathf.Max(1f, pageSize.x);
        pageSize.y = Mathf.Max(1f, pageSize.y);
        fontSize = Mathf.Max(1f, fontSize);
        textPadding = Mathf.Max(0f, textPadding);
        charactersPerPage = Mathf.Max(1, charactersPerPage);
        charactersPerLine = Mathf.Clamp(
            charactersPerLine,
            1,
            charactersPerPage);
        pageSlideDuration = Mathf.Max(0.01f, pageSlideDuration);
        maximumVisibleStackOffset = Mathf.Max(
            0,
            maximumVisibleStackOffset);
        stackRotationStep = Mathf.Max(0f, stackRotationStep);
    }
}
