using UnityEngine;
using UnityEngine.UI;

public class BarFillEffect : MonoBehaviour
{
    // 피로도 UI 이미지 객체
    public Image fatigueBarImage;
    // 공부량 UI 이미지 객체
    public Image studyBarImage;

    // 게임 밸런스 기반 최대 공부량 수치
    private float maxStudyAmount = 1f;

    // 목표 피로도 비율 수치
    private float targetFatigueRatio = 0f;
    // 목표 공부량 비율 수치
    private float targetStudyRatio = 0f;
    // UI 갱신 속도 수치
    private float fillSpeed = 5f;

    // 피로도 증가 주기 (초)
    private float fatigueInterval = 2f;
    // 누적 시간 측정용 변수
    private float elapsedTime = 0f;

    private void OnEnable()
    {
        if (PlayerStatus.Instance != null)
        {
            // 상태 변경 이벤트 구독 처리
            PlayerStatus.Instance.FatigueChanged += HandleFatigueChanged;
            PlayerStatus.Instance.StudyAmountChanged += HandleStudyAmountChanged;

            // 게임 밸런스 설정값 참조 및 최대 공부량 산출 작업
            if (PlayerStatus.Instance.Settings != null)
            {
                float studyPerGame = PlayerStatus.Instance.Settings.StudyAmountPerMiniGame;
                float gamesPerDay = PlayerStatus.Instance.Settings.NormalMiniGamesPerDay;
                float totalDays = PlayerStatus.Instance.Settings.FinalDay;
                
                // 전체 누적 최대 공부량 갱신
                maxStudyAmount = studyPerGame * gamesPerDay * totalDays;
            }

            // 초기 비율 설정 시 0 나누기 방지 조건 추가
            if (PlayerStatus.Instance.MaxFatigue > 0)
            {
                targetFatigueRatio = PlayerStatus.Instance.Fatigue / PlayerStatus.Instance.MaxFatigue;
            }
            
            if (maxStudyAmount > 0)
            {
                targetStudyRatio = PlayerStatus.Instance.StudyAmount / maxStudyAmount;
            }

            // 초기 UI 채우기 즉시 적용
            if (fatigueBarImage != null) fatigueBarImage.fillAmount = targetFatigueRatio;
            if (studyBarImage != null) studyBarImage.fillAmount = targetStudyRatio;
        }
    }

    private void OnDisable()
    {
        if (PlayerStatus.Instance != null)
        {
            // 메모리 누수 방지용 이벤트 구독 해제 작업
            PlayerStatus.Instance.FatigueChanged -= HandleFatigueChanged;
            PlayerStatus.Instance.StudyAmountChanged -= HandleStudyAmountChanged;
        }
    }

    private void Update()
    {
        // 피로도 바 UI 보간 연출 처리
        if (fatigueBarImage != null)
        {
            fatigueBarImage.fillAmount = Mathf.Lerp(fatigueBarImage.fillAmount, targetFatigueRatio, Time.deltaTime * fillSpeed);
        }

        // 공부량 바 UI 보간 연출 처리
        if (studyBarImage != null)
        {
            studyBarImage.fillAmount = Mathf.Lerp(studyBarImage.fillAmount, targetStudyRatio, Time.deltaTime * fillSpeed);
        }

        // 2초마다 피로도 증가시키는 타이머 로직 실행
        if (PlayerStatus.Instance != null && !PlayerStatus.Instance.IsGameOver)
        {
            // 매 프레임 경과 시간 누적 계산
            elapsedTime += Time.deltaTime;

            // 2초 주기 경과 여부 확인
            if (elapsedTime >= fatigueInterval)
            {
                // 주기 시간 차감 처리
                elapsedTime -= fatigueInterval;

                // 피로도 1 증가 함수 호출 실행
                PlayerStatus.Instance.AddFatigue(1f);
            }
        }
    }

    // 피로도 변경 이벤트 호출 시 실행
    private void HandleFatigueChanged(float current, float max)
    {
        if (max > 0)
        {
            // 목표 피로도 비율 갱신 처리
            targetFatigueRatio = current / max;
        }
    }

    // 공부량 변경 이벤트 호출 시 실행
    private void HandleStudyAmountChanged(int amount)
    {
        if (maxStudyAmount > 0)
        {
            // 목표 공부량 비율 갱신 처리
            targetStudyRatio = amount / maxStudyAmount;
        }
    }
}