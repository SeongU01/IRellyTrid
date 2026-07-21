using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SortingMiniGame : MiniGameBase
{
    private enum SortSide
    {
        Left,
        Right
    }

    [Header("Categories")]
    [SerializeField] private SortCategoryData[] categories;

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

    private readonly List<int> activeCategoryIndices =
        new List<int>();
    private readonly List<int> categoryBag = new List<int>();
    private readonly List<int> itemQueue = new List<int>();
    private readonly List<Image> itemViews = new List<Image>();
    private readonly List<SortingCategoryView> guideViews =
        new List<SortingCategoryView>();
    private readonly Dictionary<int, SortSide> categorySides =
        new Dictionary<int, SortSide>();

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

    protected override void OnStart()
    {
        currentDifficulty =
            DayDifficultySelector.GetForDay(
                dayDifficulties,
                CurrentDay);

        if (!ValidateSettings())
        {
            Fail();
            return;
        }

        sortedCount = 0;
        mistakeCount = 0;
        lastGeneratedCategoryIndex = -1;

        ClearRuntimeData();
        AssignCategorySides();
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

        activeCategoryCount = Mathf.Max(
            2,
            currentDifficulty.categoryCount);

        if (categories == null ||
            categories.Length < activeCategoryCount)
        {
#if UNITY_EDITOR
            Debug.LogError(
                $"SortingMiniGame needs {activeCategoryCount} " +
                $"categories, but only {categories?.Length ?? 0} are registered.");
#endif
            return false;
        }

        for (int i = 0; i < activeCategoryCount; i++)
        {
            if (categories[i] == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"Sorting category {i + 1} is empty.");
#endif
                return false;
            }
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

        for (int i = 0; i < activeCategoryCount; i++)
        {
            activeCategoryIndices.Add(i);
        }
    }

    private void AssignCategorySides()
    {
        Shuffle(activeCategoryIndices);

        for (int i = 0; i < activeCategoryIndices.Count; i++)
        {
            int categoryIndex = activeCategoryIndices[i];
            categorySides[categoryIndex] = i % 2 == 0
                ? SortSide.Left
                : SortSide.Right;
        }
    }

    private void SubmitSide(SortSide selectedSide)
    {
        if (!IsPlaying || itemQueue.Count == 0)
        {
            return;
        }

        int currentCategoryIndex = itemQueue[0];
        SortSide answer = categorySides[currentCategoryIndex];

        if (selectedSide != answer)
        {
            mistakeCount++;
            UpdateStatus("X");

            if (mistakeCount >= allowedMistakes)
            {
                Fail();
            }

            return;
        }

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
        RefreshItemQueueViews();
        UpdateStatus("O");
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
            new Vector2(-190f, -70f);

        mistakeText = CreateText(
            "MistakeText",
            canvasRoot,
            new Vector2(0.5f, 1f),
            new Vector2(320f, 60f),
            32f);
        mistakeText.rectTransform.anchoredPosition =
            new Vector2(190f, -70f);

        resultText = CreateText(
            "ResultText",
            canvasRoot,
            new Vector2(0.5f, 0f),
            new Vector2(260f, 55f),
            36f);
        resultText.rectTransform.anchoredPosition =
            new Vector2(0f, 95f);

        CreateGuideViews(SortSide.Left, -430f);
        CreateGuideViews(SortSide.Right, 430f);
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
            360f);
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

        float step = guideSize.y + guideSpacing;

        for (int i = 0; i < sideCategories.Count; i++)
        {
            int categoryIndex = sideCategories[i];
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
                xPosition,
                (i - (sideCategories.Count - 1) * 0.5f) * step + 45f);

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
            view.Initialize(icon, label, categories[categoryIndex]);
            guideViews.Add(view);
        }
    }

    private void CreateItemQueueViews()
    {
        int viewCount = Mathf.Max(1, visibleQueueCount);
        float step = itemSize.y + queueSpacing;

        for (int i = 0; i < viewCount; i++)
        {
            Image itemView = CreateImage(
                $"QueueItem_{i + 1}",
                canvasRoot,
                new Vector2(0.5f, 0f),
                itemSize);
            itemView.rectTransform.anchoredPosition =
                new Vector2(0f, 185f + i * step);
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
            new Vector2(0f, 135f);
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
            itemView.sprite = category.sprite;
            itemView.color = category.sprite != null
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
        text.color = Color.white;
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
