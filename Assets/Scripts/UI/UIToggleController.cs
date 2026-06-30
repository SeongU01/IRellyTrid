using UnityEngine;
using UnityEngine.UI;

// UI 토글 버튼 제어 클래스
public class UIToggleController : MonoBehaviour
{
    // 제어 대상 버튼 컴포넌트
    public Button OptionButton;

    // UI 토글 로직 참조
    public UIToggleLogic logic;

    // 켜고 끌 대상 UI 패널
    public GameObject optionPanel;

    void Start()
    {
        RegisterToggleEvent();
    }

    // 토글 버튼 이벤트 등록 모듈
    private void RegisterToggleEvent()
    {
        if (OptionButton == null || logic == null || optionPanel == null)
        {
            // 필수 할당 컴포넌트 누락 경고
            Debug.LogWarning("필수 컴포넌트 누락");
            return;
        }

        // 기존 이벤트 리스너 제거
        OptionButton.onClick.RemoveAllListeners();

        // 람다식을 이용한 패널 토글 액션 추가
        OptionButton.onClick.AddListener(() => logic.ToggleVisibility(optionPanel));
    }
}