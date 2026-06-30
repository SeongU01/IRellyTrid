using UnityEngine;
using UnityEngine.UI;

// 버튼과 씬 전환 로직을 연결하는 컨트롤러 클래스
public class SceneButtonController : MonoBehaviour
{
    // 대상 버튼 컴포넌트
    public Button transitionButton;
    
    // 씬 로직 컴포넌트 참조
    public SceneChangeLogic sceneLogic;
    
    // 이동할 씬 이름
    public string targetSceneName;

    void Start()
    {
        RegisterSceneChangeEvent();
    }

    // 버튼 이벤트 등록 모듈
    private void RegisterSceneChangeEvent()
    {
        if (transitionButton == null || sceneLogic == null)
        {
            // 필수 컴포넌트 누락 경고 출력
            Debug.LogWarning("필수 컴포넌트 누락");
            return;
        }

        // 기존 리스너 제거
        transitionButton.onClick.RemoveAllListeners();
        
        // 람다식을 이용한 씬 전환 액션 추가
        transitionButton.onClick.AddListener(() => sceneLogic.LoadSceneByName(targetSceneName));
    }
}