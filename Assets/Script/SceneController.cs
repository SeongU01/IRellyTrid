using UnityEngine;
using UnityEngine.SceneManagement; // Scene 관리 모듈 참조
using UnityEngine.UI;
using TMPro;

public class SceneController : MonoBehaviour
{
    // 대상 Scene 이름 입력란
    public string targetSceneName = "GameScene";
    [SerializeField] private bool createQuitButton = true;
    [SerializeField] private bool quitApplication;
    [SerializeField] private string quitButtonLabel = "QUIT";
    [SerializeField] private float quitButtonSpacing = 20f;

    private void Start()
    {
        if (!createQuitButton)
            return;

        GameObject quitButton = Instantiate(gameObject, transform.parent);
        quitButton.name = "GameQuitButton";

        SceneController quitController =
            quitButton.GetComponent<SceneController>();
        quitController.createQuitButton = false;
        quitController.quitApplication = true;

        RectTransform sourceRect = GetComponent<RectTransform>();
        RectTransform quitRect = quitButton.GetComponent<RectTransform>();
        quitRect.anchoredPosition = sourceRect.anchoredPosition -
            Vector2.up * (sourceRect.rect.height + quitButtonSpacing);

        TMP_Text label = quitButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = quitButtonLabel;

        Button button = quitButton.GetComponent<Button>();
        button.interactable = true;
    }

    // 대상 Scene 로드 함수
    public void LoadTargetScene()
    {
        if (quitApplication)
        {
            QuitGame();
            return;
        }

        // Scene 전환 실행
        SceneManager.LoadScene(targetSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
