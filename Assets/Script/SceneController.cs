using UnityEngine;
using UnityEngine.SceneManagement; // Scene 관리 모듈 참조

public class SceneController : MonoBehaviour
{
    // 대상 Scene 이름 입력란
    public string targetSceneName = "GameScene";
    [SerializeField] private bool quitApplication;

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
