using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SortingMiniGame : MiniGameBase
{
    private const float BoardCenterX = -165f;
    private const int MaxGuideRows = 3;
    private const float GuideColumnSpacing = 140f;

    [Header("Categories")]
    [SerializeField] private SortCategoryData[] categories;

    [Header("Category Asset Variants")]
    [SerializeField] private Sprite[] firstBookSprites;
    [SerializeField] private Sprite[] secondBookSprites;
    [SerializeField] private Sprite[] firstObjectSprites;
    [SerializeField] private Sprite[] secondObjectSprites;

    [Header("Day Difficulties")]
    [SerializeField]
    private SortingDayDifficulty[] dayDifficulties;

    [Header("Optional Button Sprites")]
    [SerializeField] private Sprite leftButtonSprite;
    [SerializeField] private Sprite rightButtonSprite;

    [Header("Layout")]
    [SerializeField] private Vector2 itemSize = new Vector2(92f, 92f);
    [SerializeField] private Vector2 guideSize = new Vector2(110f, 125f);
    [SerializeField] private Vector2 sortButtonSize = new Vector2(125f, 95f);
    [SerializeField, Min(1)] private int visibleQueueCount = 6;
    [SerializeField] private float queueSpacing = 10f;
    [SerializeField] private float guideSpacing = 18f;

    [Header("Animation")]
    [SerializeField, Min(0.05f)]
    private float queueMoveDuration = 0.2f;

    private readonly List<int> activeCategoryIndices =
        new List<int>();
    private readonly List<int> categoryBag = new List<int>();
    private readonly List<int> itemQueue = new List<int>();
    private readonly List<Image> itemViews = new List<Image>();
    private readonly List<SortingCategoryView> guideViews =
        new List<SortingCategoryView>();
    private readonly Dictionary<int, SortSide> categorySides =
        new Dictionary<int, SortSide>();
    private readonly Dictionary<int, Sprite> resolvedCategorySprites =
        new Dictionary<int, Sprite>();
    private readonly List<Sprite> firstBookSpriteBag =
        new List<Sprite>();
    private readonly List<Sprite> secondBookSpriteBag =
        new List<Sprite>();
    private readonly List<Sprite> firstObjectSpriteBag =
        new List<Sprite>();
    private readonly List<Sprite> secondObjectSpriteBag =
        new List<Sprite>();

    private SortingDayDifficulty currentDifficulty;
    private RectTransform canvasRoot;
    private TMP_Text progressText;
    private TMP_Text mistakeText;
    private TMP_Text resultText;

    private int activeCategoryCount;
    private int requiredSortCount;
    private int allowedMistakes;
    private int sortedCount;
    private int mistakeCount;
    private int lastGeneratedCategoryIndex = -1;
    private bool isQueueAnimating;

    protected override void OnStart()
    {
        currentDifficulty =
            DayDifficultySelector.GetForDay(
                dayDifficulties,
                DifficultyDay);

        if (!ValidateSettings())
        {
            Fail();
            return;
        }

        sortedCount = 0;
        mistakeCount = 0;
        lastGeneratedCategoryIndex = -1;
        isQueueAnimating = false;

        ClearRuntimeData();
        AssignConfiguredCategorySides();
        ResolveCategorySprites();
        FillItemQueue();
        BuildInterface();
        RefreshItemQueueViews();
        UpdateStatus("");
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            SubmitSide(SortSide.Left);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            SubmitSide(SortSide.Right);
        }
    }

    private bool ValidateSettings()
    {
        if (currentDifficulty == null)
        {
#if UNITY_EDITOR
            Debug.LogError("SortingMiniGame day difficulty is missing.");
#endif
            return false;
        }

        if (categories == null || categories.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogError("SortingMiniGame has no registered categories.");
#endif
            return false;
        }

        int[] configuredCategoryIndices =
            currentDifficulty.categoryIndices;

        if (configuredCategoryIndices == null ||
            configuredCategoryIndices.Length < 2)
        {
#if UNITY_EDITOR
            Debug.LogError(
                "SortingMiniGame needs at least two category indices " +
                "for the current day.");
#endif
            return false;
        }

        activeCategoryCount = configuredCategoryIndices.Length;

        int leftCategoryCount = 0;
        int rightCategoryCount = 0;
        HashSet<int> uniqueCategoryIndices = new HashSet<int>();

        for (int i = 0; i < activeCategoryCount; i++)
        {
            int categoryIndex = configuredCategoryIndices[i];

            if (categoryIndex < 0 || categoryIndex >= categories.Length)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"Sorting category index {categoryIndex} for the current day " +
                    $"is outside the valid range 0-{categories.Length - 1}.");
#endif
                return false;
            }

            if (!uniqueCategoryIndices.Add(categoryIndex))
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"Sorting category index {categoryIndex} is registered " +
                    "more than once for the current day.");
#endif
                return false;
            }

            SortCategoryData category = categories[categoryIndex];

            if (category == null)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"Sorting category {categoryIndex} is empty.");
#endif
                return false;
            }

            if (category.side == SortSide.Left)
            {
                leftCategoryCount++;
            }
            else
            {
                rightCategoryCount++;
            }
        }

        if (leftCategoryCount == 0 || rightCategoryCount == 0)
        {
#if UNITY_EDITOR
            Debug.LogError(
                "SortingMiniGame needs at least one active category " +
                "assigned to each side.");
#endif
            return false;
        }

        requiredSortCount = Mathf.Max(
            1,
            currentDifficulty.requiredSortCount);
        allowedMistakes = Mathf.Max(
            1,
            currentDifficulty.allowedMistakes);

        return true;
    }

    private void ClearRuntimeData()
    {
        activeCategoryIndices.Clear();
        categoryBag.Clear();
        itemQueue.Clear();
        itemViews.Clear();
        guideViews.Clear();
        categorySides.Clear();
        resolvedCategorySprites.Clear();
        firstBookSpriteBag.Clear();
        secondBookSpriteBag.Clear();
        firstObjectSpriteBag.Clear();
        secondObjectSpriteBag.Clear();

        int[] configuredCategoryIndices =
            currentDifficulty.categoryIndices;

        for (int i = 0; i < configuredCategoryIndices.Length; i++)
        {
            activeCategoryIndices.Add(configuredCategoryIndices[i]);
        }
    }

    private void AssignConfiguredCategorySides()
    {
        for (int i = 0; i < activeCategoryIndices.Count; i++)
        {
            int categoryIndex = activeCategoryIndices[i];
            categorySides[categoryIndex] =
                categories[categoryIndex].side;
        }
    }

    private void ResolveCategorySprites()
    {
        for (int i = 0; i < activeCategoryIndices.Count; i++)
        {
            int categoryIndex = activeCategoryIndices[i];
            SortCategoryData category = categories[categoryIndex];
            Sprite resolvedSprite = category.sprite;

            int variantTier = SelectVariantTier();
            if (variantTier == 1)
            {
                resolvedSprite = TakeVariantSprite(
                    category.side == SortSide.Left
                        ? firstBookSprites
                        : firstObjectSprites,
                    category.side == SortSide.Left
                        ? firstBookSpriteBag
                        : firstObjectSpriteBag) ?? category.sprite;
            }
            else if (variantTier == 2)
            {
                resolvedSprite = TakeVariantSprite(
                    category.side == SortSide.Left
                        ? secondBookSprites
                        : secondObjectSprites,
                    category.side == SortSide.Left
                        ? secondBookSpriteBag
                        : secondObjectSpriteBag) ?? category.sprite;
            }

            resolvedCategorySprites[categoryIndex] = resolvedSprite;
        }
    }

    private int SelectVariantTier()
    {
        if (DifficultyDay < 5)
            return 0;

        return DifficultyDay >= 6
            ? Random.Range(0, 3)
            : Random.Range(0, 2);
    }

    private static Sprite TakeVariantSprite(
        Sprite[] source,
        List<Sprite> bag)
    {
        if (bag.Count == 0 && source != null)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                    bag.Add(source[i]);
            }

            for (int i = bag.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (bag[i], bag[swapIndex]) =
                    (bag[swapIndex], bag[i]);
            }
        }

        if (bag.Count == 0)
            return null;

        int lastIndex = bag.Count - 1;
        Sprite sprite = bag[lastIndex];
        bag.RemoveAt(lastIndex);
        return sprite;
    }

    private void SubmitSide(SortSide selectedSide)
    {
        if (!IsPlaying ||
            isQueueAnimating ||
            itemQueue.Count == 0)
        {
            return;
        }

        int currentCategoryIndex = itemQueue[0];
        SortSide answer = categorySides[currentCategoryIndex];

        if (selectedSide != answer)
        {
            ShowWrongFeedback();
            mistakeCount++;
            UpdateStatus("X");

            if (mistakeCount >= allowedMistakes)
            {
                Fail();
            }

            return;
        }

        GameAudioManager.PlaySortingCorrect();
        ShowCorrectFeedback();
        sortedCount++;
        itemQueue.RemoveAt(0);

        if (sortedCount >= requiredSortCount)
        {
            RefreshItemQueueViews();
            UpdateStatus("CLEAR");
            Success();
            return;
        }

        FillItemQueue();
        UpdateStatus("O");
        StartCoroutine(AnimateQueueDown());
    }

    private IEnumerator AnimateQueueDown()
    {
        isQueueAnimating = true;

        int oldVisibleCount = 0;

        for (int i = 0; i < itemViews.Count; i++)
        {
            if (itemViews[i].enabled)
            {
                oldVisibleCount++;
            }
        }

        if (oldVisibleCount > 0)
        {
            itemViews[0].enabled = false;
        }

        float duration = Mathf.Max(0.05f, queueMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float ratio = Mathf.Clamp01(elapsed / duration);
            float easedRatio =
                1f - Mathf.Pow(1f - ratio, 3f);

            for (int i = 1; i < oldVisibleCount; i++)
            {
                itemViews[i].rectTransform.anchoredPosition =
                    Vector2.Lerp(
                        GetQueueItemPosition(i),
                        GetQueueItemPosition(i - 1),
                        easedRatio);
            }

            yield return null;
        }

        for (int i = 0; i < itemViews.Count; i++)
        {
            itemViews[i].rectTransform.anchoredPosition =
                GetQueueItemPosition(i);
        }

        RefreshItemQueueViews();
        isQueueAnimating = false;
    }

    private void FillItemQueue()
    {
        int queueLimit = Mathf.Max(1, visibleQueueCount);

        while (itemQueue.Count < queueLimit &&
               sortedCount + itemQueue.Count < requiredSortCount)
        {
            itemQueue.Add(GetNextCategoryIndex());
        }
    }

    private int GetNextCategoryIndex()
    {
        if (categoryBag.Count == 0)
        {
            categoryBag.AddRange(activeCategoryIndices);
            Shuffle(categoryBag);

            if (categoryBag.Count > 1 &&
                categoryBag[categoryBag.Count - 1] ==
                lastGeneratedCategoryIndex)
            {
                int swapIndex = Random.Range(
                    0,
                    categoryBag.Count - 1);
                int lastIndex = categoryBag.Count - 1;

                (categoryBag[swapIndex], categoryBag[lastIndex]) =
                    (categoryBag[lastIndex], categoryBag[swapIndex]);
            }
        }

        int index = categoryBag.Count - 1;
        int categoryIndex = categoryBag[index];
        categoryBag.RemoveAt(index);
        lastGeneratedCategoryIndex = categoryIndex;
        return categoryIndex;
    }

    private void Shuffle(List<int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (values[i], values[randomIndex]) =
                (values[randomIndex], values[i]);
        }
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        canvasRoot = canvasObject.GetComponent<RectTransform>();
        canvasRoot.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        progressText = CreateText(
            "ProgressText",
            canvasRoot,
            new Vector2(0.5f, 1f),
            new Vector2(320f, 60f),
            32f);
        progressText.rectTransform.anchoredPosition =
            new Vector2(-340f, -350f);

        mistakeText = CreateText(
            "MistakeText",
            canvasRoot,
            new Vector2(0.5f, 1f),
            new Vector2(320f, 60f),
            32f);
        mistakeText.rectTransform.anchoredPosition =
            new Vector2(10f, -350f);

        resultText = CreateText(
            "ResultText",
            canvasRoot,
            new Vector2(0.5f, 0f),
            new Vector2(260f, 55f),
            36f);
        resultText.rectTransform.anchoredPosition =
            new Vector2(BoardCenterX, 95f);

        CreateGuideViews(SortSide.Left, -430f);
        CreateGuideViews(SortSide.Right, 100f);
        CreateItemQueueViews();
        CreateSortButton(
            "LeftButton",
            SortSide.Left,
            leftButtonSprite,
            "LEFT",
            -360f);
        CreateSortButton(
            "RightButton",
            SortSide.Right,
            rightButtonSprite,
            "RIGHT",
            30f);
    }

    private void CreateGuideViews(SortSide side, float xPosition)
    {
        List<int> sideCategories = new List<int>();

        for (int i = 0; i < activeCategoryIndices.Count; i++)
        {
            int categoryIndex = activeCategoryIndices[i];

            if (categorySides[categoryIndex] == side)
            {
                sideCategories.Add(categoryIndex);
            }
        }

        int columnCount = Mathf.CeilToInt(
            sideCategories.Count / (float)MaxGuideRows);
        int rowsPerColumn = Mathf.CeilToInt(
            sideCategories.Count / (float)columnCount);
        float step = guideSize.y + guideSpacing;

        for (int i = 0; i < sideCategories.Count; i++)
        {
            int categoryIndex = sideCategories[i];
            int columnIndex = i / rowsPerColumn;
            int rowIndex = i % rowsPerColumn;
            int rowCount = Mathf.Min(
                rowsPerColumn,
                sideCategories.Count - columnIndex * rowsPerColumn);
            float columnOffset =
                (columnIndex - (columnCount - 1) * 0.5f) *
                GuideColumnSpacing;
            GameObject guideObject = new GameObject(
                $"{side}Guide_{i + 1}",
                typeof(RectTransform),
                typeof(SortingCategoryView));

            RectTransform guideRect =
                guideObject.GetComponent<RectTransform>();
            guideRect.SetParent(canvasRoot, false);
            guideRect.anchorMin = new Vector2(0.5f, 0.5f);
            guideRect.anchorMax = new Vector2(0.5f, 0.5f);
            guideRect.sizeDelta = guideSize;
            guideRect.anchoredPosition = new Vector2(
                xPosition + columnOffset,
                (rowIndex - (rowCount - 1) * 0.5f) * step - 128f);

            Image icon = CreateImage(
                "GuideImage",
                guideRect,
                new Vector2(0.5f, 0.58f),
                new Vector2(88f, 88f));

            TMP_Text label = CreateText(
                "GuideLabel",
                guideRect,
                new Vector2(0.5f, 0f),
                new Vector2(guideSize.x + 30f, 30f),
                18f);
            label.rectTransform.anchoredPosition =
                new Vector2(0f, 15f);

            SortingCategoryView view =
                guideObject.GetComponent<SortingCategoryView>();
            view.Initialize(
                icon,
                label,
                categories[categoryIndex],
                resolvedCategorySprites[categoryIndex]);
            guideViews.Add(view);
        }
    }

    private void CreateItemQueueViews()
    {
        int viewCount = Mathf.Max(1, visibleQueueCount);

        for (int i = 0; i < viewCount; i++)
        {
            Image itemView = CreateImage(
                $"QueueItem_{i + 1}",
                canvasRoot,
                new Vector2(0.5f, 0f),
                itemSize);
            itemView.rectTransform.anchoredPosition =
                GetQueueItemPosition(i);
            itemViews.Add(itemView);
        }

        TMP_Text currentMarker = CreateText(
            "CurrentMarker",
            canvasRoot,
            new Vector2(0.5f, 0f),
            new Vector2(220f, 35f),
            20f);
        currentMarker.text = "CURRENT";
        currentMarker.rectTransform.anchoredPosition =
            new Vector2(BoardCenterX, 135f);
    }

    private Vector2 GetQueueItemPosition(int index)
    {
        float step = itemSize.y + queueSpacing;
        return new Vector2(
            BoardCenterX,
            185f + index * step);
    }

    private void RefreshItemQueueViews()
    {
        for (int i = 0; i < itemViews.Count; i++)
        {
            Image itemView = itemViews[i];
            bool hasItem = i < itemQueue.Count;
            itemView.enabled = hasItem;

            if (!hasItem)
            {
                continue;
            }

            SortCategoryData category = categories[itemQueue[i]];
            Sprite resolvedSprite =
                resolvedCategorySprites[itemQueue[i]];
            itemView.sprite = resolvedSprite;
            itemView.color = resolvedSprite != null
                ? Color.white
                : category.fallbackColor;
            itemView.preserveAspect = true;
        }
    }

    private void CreateSortButton(
        string objectName,
        SortSide side,
        Sprite sprite,
        string fallbackText,
        float xPosition)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));

        RectTransform buttonRect =
            buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(canvasRoot, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = sortButtonSize;
        buttonRect.anchoredPosition = new Vector2(xPosition, 85f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = sprite;
        buttonImage.color = sprite != null
            ? Color.white
            : new Color(0.18f, 0.22f, 0.3f, 0.95f);
        buttonImage.preserveAspect = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => SubmitSide(side));

        if (sprite == null)
        {
            TMP_Text buttonText = CreateText(
                "Label",
                buttonRect,
                new Vector2(0.5f, 0.5f),
                sortButtonSize,
                27f);
            buttonText.text = fallbackText;
        }
    }

    private Image CreateImage(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 size)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform imageRect =
            imageObject.GetComponent<RectTransform>();
        imageRect.SetParent(parent, false);
        imageRect.anchorMin = anchor;
        imageRect.anchorMax = anchor;
        imageRect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 size,
        float fontSize)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.anchorMin = anchor;
        textRect.anchorMax = anchor;
        textRect.sizeDelta = size;

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;
        return text;
    }

    private void UpdateStatus(string result)
    {
        progressText.text =
            $"Sorted {sortedCount} / {requiredSortCount}";
        mistakeText.text =
            $"Mistakes {mistakeCount} / {allowedMistakes}";
        resultText.text = result;
    }

    protected override void OnEnd()
    {
        StopAllCoroutines();
        isQueueAnimating = false;

        activeCategoryIndices.Clear();
        categoryBag.Clear();
        itemQueue.Clear();
        itemViews.Clear();
        guideViews.Clear();
        categorySides.Clear();

        if (canvasRoot != null)
        {
            Destroy(canvasRoot.gameObject);
            canvasRoot = null;
        }
    }
}
