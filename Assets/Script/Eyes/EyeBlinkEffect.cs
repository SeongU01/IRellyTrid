using UnityEngine;
using UnityEngine.UI;

public class EyeBlinkEffect : MonoBehaviour
{
    // 뜬 눈 이미지 연결
    public Image openedEyesImage;
    // 감은 눈 이미지 연결
    public Image closedEyesImage;

    // 완전한 눈 감김 목표 시간
    private float maxCloseTime = 20f;
    // 현재 눈 감김 누적 시간
    private float currentCloseTime = 0f;
    // 스페이스 키 입력 시 회복 시간
    private float recoveryTime = 2f;

    private void Update()
    {
        // 매 프레임 시간 누적
        currentCloseTime += Time.deltaTime;
        
        // 스페이스 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 누적 시간 차감 적용
            currentCloseTime -= recoveryTime;
        }

        // 시간값 범위 제한
        currentCloseTime = Mathf.Clamp(currentCloseTime, 0f, maxCloseTime);

        // 현재 진행도 비율 계산
        float fillRatio = currentCloseTime / maxCloseTime;

        // 눈 이미지 비율 갱신 호출
        UpdateEyeRatio(fillRatio);
    }

    // 이미지 비율 갱신
    private void UpdateEyeRatio(float ratio)
    {
        if (closedEyesImage != null)
        {
            // 감은 눈 비율 적용
            closedEyesImage.fillAmount = ratio;
        }
        
        if (openedEyesImage != null)
        {
            // 뜬 눈 반전 비율 적용
            openedEyesImage.fillAmount = 1f - ratio;
        }
    }
}