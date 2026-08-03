using UnityEngine;

public class OptionWindowManager : MonoBehaviour
{
    // 옵션 창의 GameObject 할당 변수
    public GameObject optionPanel;

    // 옵션 창 활성화 처리
    public void OpenOptionWindow()
    {
        if (optionPanel != null)
        {
            // 옵션 창 활성화
            optionPanel.SetActive(true);
        }
    }

    // 옵션 창 비활성화 처리
    public void CloseOptionWindow()
    {
        if (optionPanel != null)
        {
            // 옵션 창 비활성화
            optionPanel.SetActive(false);
        }
    }
}