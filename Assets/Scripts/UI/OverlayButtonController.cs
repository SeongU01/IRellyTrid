using UnityEngine;
using UnityEngine.UI;

// 오버레이 버튼 제어 클래스
public class OverlayButtonController : MonoBehaviour
{
    // 제어 대상 버튼
    public Button overlayButton;
    
    // 씬 로직 참조
    public OverlaySceneLogic logic;
    
    // 대상 씬 이름
    public string targetSceneName;
    
    // 닫기 버튼 여부 설정 플래그
    public bool isCloseButton;

    void Start()
    {
        RegisterOverlayEvent();
    }

    // 오버레이 버튼 이벤트 등록 모듈
    private void RegisterOverlayEvent()
    {
        // 컴포넌트 유효성 검사
        if (overlayButton == null || logic == null)
        {
            // 경고 로그 출력
            Debug.LogWarning("필수 컴포넌트 누락");
            return;
        }

        // 기존 이벤트 제거
        overlayButton.onClick.RemoveAllListeners();

        // 동작 모드 분기
        if (isCloseButton)
        {
            // 씬 해제 액션 등록
            overlayButton.onClick.AddListener(() => logic.UnloadOverlayScene(targetSceneName));
        }
        else
        {
            // 씬 추가 액션 등록
            overlayButton.onClick.AddListener(() => logic.LoadSceneAdditive(targetSceneName));
        }
    }
}