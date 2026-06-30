using UnityEngine;
using UnityEngine.SceneManagement;

// 오버레이 씬 처리 로직 클래스
public class OverlaySceneLogic : MonoBehaviour
{
    // 씬 추가 로드 모듈
    public void LoadSceneAdditive(string sceneName)
    {
        // 씬 중복 로드 방지 검사
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            // 추가 모드 씬 로드 실행
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }

    // 오버레이 씬 해제 모듈
    public void UnloadOverlayScene(string sceneName)
    {
        // 씬 로드 상태 확인
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            // 씬 비동기 해제 실행
            SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}