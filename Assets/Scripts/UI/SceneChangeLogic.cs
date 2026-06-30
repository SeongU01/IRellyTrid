using UnityEngine;
using UnityEngine.SceneManagement;

// 특정 이름의 씬으로 전환하는 로직 클래스
public class SceneChangeLogic : MonoBehaviour
{
    // 씬 전환 모듈
    public void LoadSceneByName(string sceneName)
    {
        // 입력된 이름의 씬 로드
        SceneManager.LoadScene(sceneName);
    }
}