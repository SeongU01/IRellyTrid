using UnityEngine;
using UnityEngine.SceneManagement; // Scene 관리 모듈 참조

public class SceneController : MonoBehaviour
{
    // 대상 Scene 이름 입력란
    public string targetSceneName = "GameScene";

    // 대상 Scene 로드 함수
    public void LoadTargetScene()
    {
        // Scene 전환 실행
        SceneManager.LoadScene(targetSceneName);
    }
}