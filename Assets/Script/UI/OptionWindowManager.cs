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

    public void OpenOptionWindow()
    {
        if (optionPanel == null || isOpen)
            return;

        EnsureReferences();
        EnsureBackdrop();
        EnsureTitleButton();
        RaiseCanvas();

        if (backdrop != null)
        {
            backdrop.SetActive(true);
            backdrop.transform.SetAsLastSibling();
        }

        optionPanel.SetActive(true);
        optionPanel.transform.SetAsLastSibling();
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
        CloseOptionWindow();

        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogWarning(
                $"OptionWindowManager: Scene '{titleSceneName}' cannot be loaded.");
            return;
        }

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
        button.onClick.AddListener(ReturnToTitle);
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
