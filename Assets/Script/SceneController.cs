using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Scene 관리 모듈 참조

public class SceneController : MonoBehaviour
{
    // 대상 Scene 이름 입력란
    public string targetSceneName = "GameScene";
    [SerializeField] private bool quitApplication;
    private ButtonInteractionEffect interactionEffect;
    private bool transitionRequested;

    private void Awake()
    {
        interactionEffect = ButtonInteractionEffect.Attach(
            GetComponent<Button>());
    }

    // 대상 Scene 로드 함수
    public void LoadTargetScene()
    {
        if (transitionRequested)
            return;

        transitionRequested = true;

        if (interactionEffect != null)
        {
            interactionEffect.PlayExit(PerformAction);
            return;
        }

        PerformAction();
    }

    private void PerformAction()
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
