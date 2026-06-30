using UnityEngine;

// UI 패널 활성화 제어 로직 클래스
public class UIToggleLogic : MonoBehaviour
{
    // 대상 패널의 표시 상태 전환 모듈
    public void ToggleVisibility(GameObject targetPanel)
    {
        if (targetPanel != null)
        {
            // 대상 패널의 현재 활성화 상태 확인
            bool isActive = targetPanel.activeSelf;
            
            // 현재 활성화 상태의 반전 값 적용
            targetPanel.SetActive(!isActive);
        }
    }
}