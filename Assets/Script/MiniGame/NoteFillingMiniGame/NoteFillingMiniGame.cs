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
    [SerializeField] private Vector2 pageSize =
        new Vector2(650f, 560f);

    [Header("Page Visual")]
    [SerializeField] private Sprite noteSprite;
    [SerializeField] private Color temporaryPageColor =
        new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField, Min(1f)] private float fontSize = 34f;
    [SerializeField, Min(0f)] private float textPadding = 42f;

    [Header("Writing")]
    [SerializeField, Min(1)] private int charactersPerPage = 100;
    [SerializeField, Min(1)] private int charactersPerLine = 20;

    [Header("Page Transition")]
    [SerializeField, Min(1f)] private float pageSlideDistance = 900f;
    [SerializeField, Min(0.01f)] private float pageSlideDuration = 0.35f;

    private readonly StringBuilder currentCharacters = new();
    private RectTransform pageRoot;
    private PageView currentPage;
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
        RefreshCurrentPageText();

        if (currentCharacters.Length < charactersPerPage)
            return;

        CompleteCurrentPage();
    }

    private void CompleteCurrentPage()
    {
        PageView completedPage = currentPage;
        int completedSiblingIndex = completedPage.Root.GetSiblingIndex();
        completedPageCount++;
        currentCharacters.Clear();

        currentPage = CreatePage();
        currentPage.Root.SetSiblingIndex(completedSiblingIndex);
        completedPage.Root.SetAsLastSibling();
        StartCoroutine(SlidePageRight(completedPage));

#if UNITY_EDITOR
        Debug.Log(
            $"[NoteFillingMiniGame] Page {completedPageCount} completed. " +
            $"Total characters: {totalCharacterCount}.");
#endif
    }

    private IEnumerator SlidePageRight(PageView completedPage)
    {
        if (completedPage?.Root == null)
            yield break;

        Vector2 startPosition = completedPage.Root.anchoredPosition;
        Vector2 targetPosition = startPosition +
            Vector2.right * pageSlideDistance;
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
            yield return null;
        }

        if (completedPage.Root != null)
            Destroy(completedPage.Root.gameObject);
    }

    private void CompleteGame()
    {
#if UNITY_EDITOR
        Debug.Log(
            $"[NoteFillingMiniGame] Completed pages: " +
            $"{completedPageCount}, current page: " +
            $"{currentCharacters.Length}/{charactersPerPage}, " +
            $"total characters: {totalCharacterCount}. " +
            "The next-day study multiplier is not applied yet.");
#endif

        SuccessWithStudyReward(0);
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

        GameObject rootObject = new GameObject(
            "PageRoot",
            typeof(RectTransform));
        rootObject.transform.SetParent(canvasObject.transform, false);

        pageRoot = rootObject.GetComponent<RectTransform>();
        pageRoot.anchorMin = new Vector2(0.5f, 0.5f);
        pageRoot.anchorMax = new Vector2(0.5f, 0.5f);
        pageRoot.pivot = new Vector2(0.5f, 0.5f);
        pageRoot.anchoredPosition = Vector2.zero;
        pageRoot.sizeDelta = Vector2.zero;
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
        pageRect.anchoredPosition = gameAreaCenter;
        pageRect.sizeDelta = pageSize;

        Image pageImage = pageObject.GetComponent<Image>();
        pageImage.sprite = noteSprite;
        pageImage.color = noteSprite != null
            ? Color.white
            : temporaryPageColor;
        pageImage.preserveAspect = noteSprite != null;
        pageImage.raycastTarget = false;

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
        pageText.overflowMode = TextOverflowModes.Overflow;
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
        pageSize.x = Mathf.Max(1f, pageSize.x);
        pageSize.y = Mathf.Max(1f, pageSize.y);
        fontSize = Mathf.Max(1f, fontSize);
        textPadding = Mathf.Max(0f, textPadding);
        charactersPerPage = Mathf.Max(1, charactersPerPage);
        charactersPerLine = Mathf.Clamp(
            charactersPerLine,
            1,
            charactersPerPage);
        pageSlideDistance = Mathf.Max(1f, pageSlideDistance);
        pageSlideDuration = Mathf.Max(0.01f, pageSlideDuration);
    }
}
