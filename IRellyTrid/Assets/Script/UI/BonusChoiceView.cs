using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BonusGameFlowController))]
public sealed class BonusChoiceView : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private BonusGameFlowController bonusGameFlowController;

    [Header("Button Sprites - Rest")]
    [SerializeField] private Sprite restCommonSprite;
    [SerializeField] private Sprite restHoverSprite;
    [SerializeField] private Sprite restOnClickSprite;

    [Header("Button Sprites - Bonus Mini Game")]
    [SerializeField] private Sprite bonusCommonSprite;
    [SerializeField] private Sprite bonusHoverSprite;
    [SerializeField] private Sprite bonusOnClickSprite;

    [Header("Temporary UI")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private bool showTemporaryLabels = true;
    [SerializeField] private Vector2 panelSize = new Vector2(760f, 360f);
    [SerializeField] private Vector2 buttonSize = new Vector2(280f, 110f);

    private DaySystem daySystem;
    private GameObject choicePanel;
    private TMP_Text titleText;
    private Button restButton;
    private Button bonusButton;

    private void Reset()
    {
        bonusGameFlowController = GetComponent<BonusGameFlowController>();
    }

    private void Awake()
    {
        if (bonusGameFlowController == null)
            bonusGameFlowController = GetComponent<BonusGameFlowController>();

        daySystem = DaySystem.Instance;
        CreateRuntimeView();
        ApplyButtonVisuals();
        Hide();
    }

    private void OnEnable()
    {
        daySystem = DaySystem.Instance;

        if (daySystem != null)
            daySystem.OnPhaseChanged += HandlePhaseChanged;

        Refresh(daySystem != null
            ? daySystem.CurrentPhase
            : DayPhase.NotStarted);
    }

    private void OnDisable()
    {
        if (daySystem != null)
            daySystem.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void OnValidate()
    {
        if (bonusGameFlowController == null)
            bonusGameFlowController = GetComponent<BonusGameFlowController>();

        panelSize.x = Mathf.Max(1f, panelSize.x);
        panelSize.y = Mathf.Max(1f, panelSize.y);
        buttonSize.x = Mathf.Max(1f, buttonSize.x);
        buttonSize.y = Mathf.Max(1f, buttonSize.y);

        if (Application.isPlaying)
            ApplyButtonVisuals();
    }

    private void HandlePhaseChanged(DayPhase previous, DayPhase current)
    {
        Refresh(current);
    }

    private void Refresh(DayPhase phase)
    {
        bool isFirstChoice = phase == DayPhase.FirstBonusChoice;
        bool isSecondChoice = phase == DayPhase.SecondBonusChoice;

        if (!isFirstChoice && !isSecondChoice)
        {
            Hide();
            return;
        }

        if (titleText != null)
        {
            titleText.text = isFirstChoice
                ? "First Bonus Choice"
                : "Second Bonus Choice";
        }

        SetButtonsInteractable(true);
        choicePanel.SetActive(true);
    }

    private void HandleRestClicked()
    {
        if (!TryLockCurrentChoice())
            return;

        if (daySystem.CurrentPhase == DayPhase.FirstBonusChoice)
            bonusGameFlowController.SkipFirstBonus();
        else if (daySystem.CurrentPhase == DayPhase.SecondBonusChoice)
            bonusGameFlowController.SkipSecondBonus();
    }

    private void HandleBonusClicked()
    {
        if (!TryLockCurrentChoice())
            return;

        if (daySystem.CurrentPhase == DayPhase.FirstBonusChoice)
            bonusGameFlowController.PlayFirstBonus();
        else if (daySystem.CurrentPhase == DayPhase.SecondBonusChoice)
            bonusGameFlowController.PlaySecondBonus();
    }

    private bool TryLockCurrentChoice()
    {
        if (daySystem == null || bonusGameFlowController == null)
            return false;

        if (daySystem.CurrentPhase != DayPhase.FirstBonusChoice &&
            daySystem.CurrentPhase != DayPhase.SecondBonusChoice)
        {
            return false;
        }

        SetButtonsInteractable(false);
        return true;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (restButton != null)
            restButton.interactable = interactable;

        if (bonusButton != null)
            bonusButton.interactable = interactable;
    }

    private void Hide()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);
    }

    private void CreateRuntimeView()
    {
        GameObject canvasObject = new GameObject(
            "BonusChoiceCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        choicePanel = new GameObject(
            "BonusChoicePanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        choicePanel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect =
            choicePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = panelSize;

        Image panelImage = choicePanel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        titleText = CreateText(
            choicePanel.transform,
            "Title",
            new Vector2(0f, 100f),
            new Vector2(panelSize.x - 80f, 80f),
            42f);
        titleText.color = Color.white;

        restButton = CreateButton(
            choicePanel.transform,
            "RestButton",
            "Rest",
            new Vector2(-170f, -55f));
        restButton.onClick.AddListener(HandleRestClicked);

        bonusButton = CreateButton(
            choicePanel.transform,
            "BonusMiniGameButton",
            "Bonus Mini Game",
            new Vector2(170f, -55f));
        bonusButton.onClick.AddListener(HandleBonusClicked);
    }

    private Button CreateButton(
        Transform parent,
        string objectName,
        string label,
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
        buttonImage.color = Color.white;
        buttonImage.preserveAspect = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };

        TMP_Text labelText = CreateText(
            buttonObject.transform,
            "Label",
            Vector2.zero,
            buttonSize,
            32f);
        labelText.text = label;
        labelText.gameObject.SetActive(showTemporaryLabels);

        return button;
    }

    private TMP_Text CreateText(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize)
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
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private void ApplyButtonVisuals()
    {
        ApplyButtonVisuals(
            restButton,
            restCommonSprite,
            restHoverSprite,
            restOnClickSprite);
        ApplyButtonVisuals(
            bonusButton,
            bonusCommonSprite,
            bonusHoverSprite,
            bonusOnClickSprite);
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
