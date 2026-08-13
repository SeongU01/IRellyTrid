using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(FatigueRecoveryController))]
public sealed class DaySettlementView : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField]
    private FatigueRecoveryController fatigueRecoveryController;

    [Header("Panel Sprite")]
    [SerializeField] private Sprite panelBackgroundSprite;

    [Header("Next Day Button Sprites")]
    [SerializeField] private Sprite nextDayCommonSprite;
    [SerializeField] private Sprite nextDayHoverSprite;
    [SerializeField] private Sprite nextDayOnClickSprite;

    [Header("Temporary UI")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private bool showTemporaryButtonLabel = true;
    [SerializeField] private Vector2 panelSize = new Vector2(860f, 700f);
    [SerializeField] private Vector2 buttonSize = new Vector2(300f, 100f);

    private DaySystem daySystem;
    private PlayerStatus playerStatus;
    private GameObject settlementPanel;
    private Image panelImage;
    private TMP_Text titleText;
    private TMP_Text summaryText;
    private TMP_Text nextDayButtonLabel;
    private Button nextDayButton;
    private float requestedRecovery;
    private float actualRecovery;

    private void Reset()
    {
        fatigueRecoveryController =
            GetComponent<FatigueRecoveryController>();
    }

    private void Awake()
    {
        if (fatigueRecoveryController == null)
        {
            fatigueRecoveryController =
                GetComponent<FatigueRecoveryController>();
        }

        daySystem = DaySystem.Instance;
        playerStatus = PlayerStatus.Instance;
        CreateRuntimeView();
        ApplyVisuals();
        Hide();
    }

    private void OnEnable()
    {
        daySystem = DaySystem.Instance;
        playerStatus = PlayerStatus.Instance;

        if (daySystem != null)
        {
            daySystem.OnDayStarted += HandleDayStarted;
            daySystem.OnPhaseChanged += HandlePhaseChanged;
        }

        if (fatigueRecoveryController != null)
        {
            fatigueRecoveryController.OnRecoveryApplied +=
                HandleRecoveryApplied;
        }

        Refresh(daySystem != null
            ? daySystem.CurrentPhase
            : DayPhase.NotStarted);
    }

    private void OnDisable()
    {
        if (daySystem != null)
        {
            daySystem.OnDayStarted -= HandleDayStarted;
            daySystem.OnPhaseChanged -= HandlePhaseChanged;
        }

        if (fatigueRecoveryController != null)
        {
            fatigueRecoveryController.OnRecoveryApplied -=
                HandleRecoveryApplied;
        }
    }

    private void OnValidate()
    {
        if (fatigueRecoveryController == null)
        {
            fatigueRecoveryController =
                GetComponent<FatigueRecoveryController>();
        }

        panelSize.x = Mathf.Max(1f, panelSize.x);
        panelSize.y = Mathf.Max(1f, panelSize.y);
        buttonSize.x = Mathf.Max(1f, buttonSize.x);
        buttonSize.y = Mathf.Max(1f, buttonSize.y);

        if (Application.isPlaying)
            ApplyVisuals();
    }

    private void HandleDayStarted(int day)
    {
        requestedRecovery = 0f;
        actualRecovery = 0f;
    }

    private void HandleRecoveryApplied(
        int performedBonusGames,
        float requestedAmount,
        float actualAmount)
    {
        requestedRecovery = requestedAmount;
        actualRecovery = actualAmount;
    }

    private void HandlePhaseChanged(DayPhase previous, DayPhase current)
    {
        Refresh(current);
    }

    private void Refresh(DayPhase phase)
    {
        if (phase != DayPhase.Settlement)
        {
            Hide();
            return;
        }

        if (daySystem == null || playerStatus == null)
        {
            Hide();
            return;
        }

        titleText.text = $"Day {daySystem.CurrentDay} Settlement";
        summaryText.text =
            $"Today's Study        {daySystem.TodayStudyAmount}\n" +
            $"Total Study          {playerStatus.StudyAmount}\n" +
            $"Health Lost          {daySystem.TodayLostHealth}\n" +
            $"Current Health       {playerStatus.Health}/" +
            $"{playerStatus.MaxHealth}\n" +
            $"Current Fatigue      {playerStatus.Fatigue:0.##}/" +
            $"{playerStatus.MaxFatigue:0.##}\n" +
            $"Bonus Games          " +
            $"{daySystem.PerformedBonusMiniGames}/2\n" +
            $"Fatigue Recovered    {actualRecovery:0.##}";

        nextDayButtonLabel.text = daySystem.IsFinalDay
            ? "Finish"
            : "Next Day";
        nextDayButtonLabel.gameObject.SetActive(
            showTemporaryButtonLabel);
        nextDayButton.interactable = true;
        settlementPanel.SetActive(true);

        Debug.Log(
            $"[DaySettlementView] Day {daySystem.CurrentDay} settlement " +
            $"shown. Requested fatigue recovery: " +
            $"{requestedRecovery:0.##}, actual: {actualRecovery:0.##}.");
    }

    private void HandleNextDayClicked()
    {
        if (daySystem == null ||
            daySystem.CurrentPhase != DayPhase.Settlement ||
            !nextDayButton.interactable)
        {
            return;
        }

        ButtonInteractionEffect effect =
            ButtonInteractionEffect.Attach(nextDayButton);

        if (effect != null)
        {
            effect.PlayExit(CompleteDayTransition);
            return;
        }

        CompleteDayTransition();
    }

    private void CompleteDayTransition()
    {
        nextDayButton.interactable = false;
        bool completedFinalDay = daySystem.IsFinalDay;
        daySystem.CompleteDay();

        if (!completedFinalDay &&
            daySystem.CurrentPhase == DayPhase.DayCompleted)
        {
            daySystem.StartNextDay();
        }
    }

    private void Hide()
    {
        if (settlementPanel != null)
            settlementPanel.SetActive(false);
    }

    private void CreateRuntimeView()
    {
        GameObject canvasObject = new GameObject(
            "DaySettlementCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        settlementPanel = new GameObject(
            "DaySettlementPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        settlementPanel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect =
            settlementPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = panelSize;

        panelImage = settlementPanel.GetComponent<Image>();
        panelImage.preserveAspect = true;

        titleText = CreateText(
            settlementPanel.transform,
            "Title",
            new Vector2(0f, 275f),
            new Vector2(panelSize.x - 100f, 70f),
            44f,
            TextAlignmentOptions.Center);

        summaryText = CreateText(
            settlementPanel.transform,
            "Summary",
            new Vector2(0f, 25f),
            new Vector2(panelSize.x - 180f, 390f),
            32f,
            TextAlignmentOptions.Left);

        nextDayButton = CreateButton(
            settlementPanel.transform,
            "NextDayButton",
            new Vector2(0f, -270f));
        nextDayButton.onClick.AddListener(HandleNextDayClicked);
    }

    private Button CreateButton(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect =
            buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = buttonSize;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.preserveAspect = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };
        ButtonInteractionEffect.Attach(button);

        nextDayButtonLabel = CreateText(
            buttonObject.transform,
            "Label",
            Vector2.zero,
            buttonSize,
            32f,
            TextAlignmentOptions.Center);
        nextDayButtonLabel.color = Color.black;
        return button;
    }

    private TMP_Text CreateText(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null)
            text.font = fontAsset;

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private void ApplyVisuals()
    {
        if (panelImage != null)
        {
            panelImage.sprite = panelBackgroundSprite;
            panelImage.color = panelBackgroundSprite != null
                ? Color.white
                : new Color(0.08f, 0.08f, 0.08f, 0.94f);
        }

        ApplyButtonVisuals(
            nextDayButton,
            nextDayCommonSprite,
            nextDayHoverSprite,
            nextDayOnClickSprite);
    }

    private static void ApplyButtonVisuals(
        Button button,
        Sprite commonSprite,
        Sprite hoverSprite,
        Sprite onClickSprite)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image == null)
            return;

        image.sprite = commonSprite;

        if (commonSprite != null || hoverSprite != null ||
            onClickSprite != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = hoverSprite != null
                    ? hoverSprite
                    : commonSprite,
                pressedSprite = onClickSprite != null
                    ? onClickSprite
                    : commonSprite,
                selectedSprite = hoverSprite != null
                    ? hoverSprite
                    : commonSprite,
                disabledSprite = commonSprite
            };
            image.color = Color.white;
            return;
        }

        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = new Color(0.82f, 0.9f, 1f, 1f),
            highlightedColor = new Color(0.94f, 0.97f, 1f, 1f),
            pressedColor = new Color(0.6f, 0.74f, 0.92f, 1f),
            selectedColor = new Color(0.94f, 0.97f, 1f, 1f),
            disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
    }
}
