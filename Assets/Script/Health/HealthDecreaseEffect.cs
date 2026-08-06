using UnityEngine;
using UnityEngine.UI;

public class BatteryUIController : MonoBehaviour
{
    // 꽉 찬 배터리 UI 이미지 연결
    public Image fullBatteryImage;
    // 텅 빈 배터리 UI 이미지 연결
    public Image emptyBatteryImage;

    // 목표 배터리 비율
    private float targetBatteryRatio = 1f;
    // UI 갱신 속도
    private float fillSpeed = 5f;

    private void OnEnable()
    {
        if (PlayerStatus.Instance != null)
        {
            // 체력 변경 이벤트 구독
            PlayerStatus.Instance.HealthChanged += HandleHealthChanged;

            // 초기 비율 설정
            targetBatteryRatio = (float)PlayerStatus.Instance.Health / PlayerStatus.Instance.MaxHealth;
            
            // 초기 배터리 이미지 갱신
            UpdateBatteryUI(targetBatteryRatio);
        }
    }

    private void OnDisable()
    {
        if (PlayerStatus.Instance != null)
        {
            // 이벤트 구독 해제
            PlayerStatus.Instance.HealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        // 부드러운 비율 전환 계산
        float currentRatio = Mathf.Lerp(fullBatteryImage.fillAmount, targetBatteryRatio, Time.deltaTime * fillSpeed);
        
        // 배터리 UI 비율 갱신 호출
        UpdateBatteryUI(currentRatio);
    }

    // 체력 변경 이벤트 발생 시 호출
    private void HandleHealthChanged(int current, int max)
    {
        if (max > 0)
        {
            // 목표 배터리 비율 갱신
            targetBatteryRatio = (float)current / max;
        }
    }

    // 두 배터리 이미지의 비율 갱신
    private void UpdateBatteryUI(float ratio)
    {
        if (fullBatteryImage != null)
        {
            // 꽉 찬 배터리 비율 적용
            fullBatteryImage.fillAmount = ratio;
        }
        
        if (emptyBatteryImage != null)
        {
            // 텅 빈 배터리 반전 비율 적용
            emptyBatteryImage.fillAmount = 1f - ratio;
        }
    }
}