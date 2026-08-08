using UnityEngine;
using UnityEngine.UI;

public class EyeBlinkEffect : MonoBehaviour
{
    [SerializeField] private MiniGameManager miniGameManager;

    // 뜬 눈 이미지 배열 연결
    public Image[] openedEyesImages;
    // 감은 눈 이미지 배열 연결
    public Image[] closedEyesImages;

    // 완전한 눈 감김 목표 시간
    private float maxCloseTime;
    // 현재 눈 감김 누적 시간
    private float currentCloseTime = 0f;
    // Space 키 입력 시 회복 시간
    private float recoveryTime;

    // 완전히 감긴 상태 유지 시간
    private float fullyClosedTimer = 0f;
    // 체력 감소 간격 시간
    private float damageInterval;

    private void Start()
    {
        if (PlayerStatus.Instance != null && PlayerStatus.Instance.Settings != null)
        {
            // GameBalanceSettings 설정값 동기화
            maxCloseTime = PlayerStatus.Instance.Settings.EyeCloseDuration;
            recoveryTime = PlayerStatus.Instance.Settings.EyeRecoveryPerSpace;
            damageInterval = PlayerStatus.Instance.Settings.ClosedEyeDamageInterval;
        }

        UpdateEyeRatio(0f);
    }

    private void Update()
    {
        if (!IsMiniGamePlaying())
        {
            return;
        }

        // 매 프레임 시간 누적
        currentCloseTime += Time.deltaTime;
        
        // Space 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 누적 시간 차감 적용
            currentCloseTime -= recoveryTime;
        }

        // 시간값 범위 제한
        currentCloseTime = Mathf.Clamp(currentCloseTime, 0f, maxCloseTime);

        // 현재 진행도 비율 계산
        float fillRatio = 0f;
        if (maxCloseTime > 0)
        {
            // 0 나누기 방지 및 비율 산출
            fillRatio = currentCloseTime / maxCloseTime;
        }

        // 눈 이미지 배열 비율 갱신 호출
        UpdateEyeRatio(fillRatio);

        // 체력 감소 로직 실행
        CheckDamageCondition();
    }

    // 이미지 배열 비율 갱신
    private void UpdateEyeRatio(float ratio)
    {
        // 감은 눈 배열 순회 및 비율 적용
        if (closedEyesImages != null)
        {
            foreach (Image img in closedEyesImages)
            {
                if (img != null)
                {
                    img.fillAmount = ratio;
                }
            }
        }
        
        // 뜬 눈 배열 순회 및 반전 비율 적용
        if (openedEyesImages != null)
        {
            foreach (Image img in openedEyesImages)
            {
                if (img != null)
                {
                    img.fillAmount = 1f - ratio;
                }
            }
        }
    }

    // 눈 감김 상태에 따른 체력 감소 확인
    private void CheckDamageCondition()
    {
        // 완전한 눈 감김 상태 확인
        if (currentCloseTime >= maxCloseTime)
        {
            // 완전히 감긴 시간 누적
            fullyClosedTimer += Time.deltaTime;

            // 목표 시간 경과 확인
            if (fullyClosedTimer >= damageInterval)
            {
                if (PlayerStatus.Instance != null)
                {
                    // 플레이어 체력 감소 호출
                    PlayerStatus.Instance.TakeDamage(1);
                }
                
                // 유지 시간 초기화
                fullyClosedTimer = 0f;
                // 눈 감김 누적 시간 초기화
                currentCloseTime = 0f;
            }
        }
        else
        {
            // 유지 시간 초기화
            fullyClosedTimer = 0f;
        }
    }

    private bool IsMiniGamePlaying()
    {
        if (miniGameManager == null)
        {
            miniGameManager = FindAnyObjectByType<MiniGameManager>();
        }

        return miniGameManager != null &&
            miniGameManager.IsMiniGamePlaying;
    }
}
