using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionWindowManager : MonoBehaviour
{
    [SerializeField] private GameObject optionPanel;
    [SerializeField, Range(0f, 1f)]
    private float backdropAlpha = 0.45f;
    [SerializeField] private int modalSortingOrder = 2000;
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string titleButtonLabel = "TITLE";
    [SerializeField] private float titleButtonSpacing = 12f;

    private GameObject backdrop;
    private GameObject titleButton;
    private Canvas rootCanvas;
    private MiniGameManager miniGameManager;
    private bool canvasStateStored;
    private bool previousOverrideSorting;
    private int previousSortingOrder;
    private bool isOpen;
    private bool returningToTitle;

    private void Awake()
    {
        AttachButtonEffects();
    }

    public void OpenOptionWindow()
    {
        if (optionPanel == null || isOpen)
            return;

        EnsureReferences();
        EnsureBackdrop();
        EnsureTitleButton();
        AttachButtonEffects();
        RaiseCanvas();

        if (backdrop != null)
            backdrop.SetActive(true);

        optionPanel.SetActive(true);
        PlaceBackdropBelowPanel();
        miniGameManager?.SetPaused(true);
        isOpen = true;
    }

    public void CloseOptionWindow()
    {
        if (optionPanel != null)
            optionPanel.SetActive(false);

        if (backdrop != null)
            backdrop.SetActive(false);

        EnsureReferences();
        miniGameManager?.SetPaused(false);
        RestoreCanvas();
        isOpen = false;
    }

    public void ReturnToTitle()
    {
        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogWarning(
                $"OptionWindowManager: Scene '{titleSceneName}' cannot be loaded.");
            return;
        }

        if (returningToTitle)
            return;

        returningToTitle = true;
        Button button = titleButton != null
            ? titleButton.GetComponent<Button>()
            : null;
        ButtonInteractionEffect effect =
            ButtonInteractionEffect.Attach(button);

        if (effect != null)
        {
            effect.PlayExit(LoadTitleScene);
            return;
        }

        LoadTitleScene();
    }

    private void LoadTitleScene()
    {
        CloseOptionWindow();
        SceneManager.LoadScene(titleSceneName);
    }

    private void OnDestroy()
    {
        if (isOpen)
        {
            miniGameManager?.SetPaused(false);
            RestoreCanvas();
        }

        if (backdrop != null)
            Destroy(backdrop);
    }

    private void EnsureReferences()
    {
        if (rootCanvas == null && optionPanel != null)
            rootCanvas = optionPanel.GetComponentInParent<Canvas>(true);

        if (miniGameManager == null)
            miniGameManager = FindAnyObjectByType<MiniGameManager>();
    }

    private void EnsureBackdrop()
    {
        if (backdrop != null || rootCanvas == null)
            return;

        backdrop = new GameObject(
            "OptionBackdrop",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        backdrop.layer = rootCanvas.gameObject.layer;

        RectTransform backdropRect =
            backdrop.GetComponent<RectTransform>();
        backdropRect.SetParent(rootCanvas.transform, false);
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image backdropImage = backdrop.GetComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, backdropAlpha);
        backdropImage.raycastTarget = true;
        backdrop.SetActive(false);
    }

    private void PlaceBackdropBelowPanel()
    {
        if (backdrop == null || optionPanel == null)
            return;

        Transform panelTransform = optionPanel.transform;
        Transform panelParent = panelTransform.parent;

        if (panelParent != null && backdrop.transform.parent != panelParent)
            backdrop.transform.SetParent(panelParent, false);

        backdrop.transform.SetSiblingIndex(panelTransform.GetSiblingIndex());
        panelTransform.SetAsLastSibling();
    }

    private void EnsureTitleButton()
    {
        if (titleButton != null || optionPanel == null)
            return;

        Button closeButton = optionPanel.GetComponentInChildren<Button>(true);
        if (closeButton == null)
            return;

        titleButton = Instantiate(
            closeButton.gameObject,
            closeButton.transform.parent);
        titleButton.name = "TitleButton";

        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        RectTransform titleRect = titleButton.GetComponent<RectTransform>();
        titleRect.anchoredPosition = closeRect.anchoredPosition +
            Vector2.up * (closeRect.rect.height + titleButtonSpacing);

        TMP_Text label = titleButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = titleButtonLabel;

        Button button = titleButton.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(ReturnToTitle);
        ButtonInteractionEffect.Attach(button);
    }

    private void AttachButtonEffects()
    {
        EnsureReferences();
        Button[] buttons = rootCanvas != null
            ? rootCanvas.GetComponentsInChildren<Button>(true)
            : GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            bool belongsToPanel = optionPanel != null &&
                button.transform.IsChildOf(optionPanel.transform);
            bool invokesThisManager = false;

            for (int callIndex = 0;
                 callIndex < button.onClick.GetPersistentEventCount();
                 callIndex++)
            {
                if (button.onClick.GetPersistentTarget(callIndex) == this)
                {
                    invokesThisManager = true;
                    break;
                }
            }

            if (belongsToPanel || invokesThisManager)
                ButtonInteractionEffect.Attach(button);
        }
    }

    private void RaiseCanvas()
    {
        if (rootCanvas == null || canvasStateStored)
            return;

        previousOverrideSorting = rootCanvas.overrideSorting;
        previousSortingOrder = rootCanvas.sortingOrder;
        canvasStateStored = true;
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = modalSortingOrder;
    }

    private void RestoreCanvas()
    {
        if (rootCanvas == null || !canvasStateStored)
            return;

        rootCanvas.overrideSorting = previousOverrideSorting;
        rootCanvas.sortingOrder = previousSortingOrder;
        canvasStateStored = false;
    }
}
