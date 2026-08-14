using UnityEngine;
using UnityEngine.InputSystem;

public class FatigueController : MonoBehaviour
{
    private const float DebugFatiguePerSecond = 6.67f;

    [SerializeField] private MiniGameManager miniGameManager;

    // 누적 시간 변수
    private float elapsedTime = 0f;
    // 피로도 증가 주기 (초)
    private float increaseInterval = 1f;

    private void Update()
    {
        if (!IsMiniGamePlaying())
        {
            return;
        }

        // 플레이어 상태 인스턴스 존재 및 게임 오버 여부 확인
        if (PlayerStatus.Instance == null || PlayerStatus.Instance.IsGameOver)
        {
            return;
        }

        // 매 프레임 경과 시간 누적
        elapsedTime += Time.deltaTime;

        // 지정된 주기(1초) 경과 확인
        if (elapsedTime >= increaseInterval)
        {
            // 누적 시간 차감
            elapsedTime -= increaseInterval;

            // 게임 밸런스 설정의 초당 피로도 증가값 참조
            bool isDebugAccelerationActive = Keyboard.current != null &&
                Keyboard.current.f12Key.isPressed;
            float fatiguePerSec = isDebugAccelerationActive
                ? DebugFatiguePerSecond
                : PlayerStatus.Instance.Settings.FatiguePerSecond;

            if (!isDebugAccelerationActive &&
                miniGameManager.IsBonusMiniGamePlaying)
            {
                fatiguePerSec *= 0.5f;
            }

            // 플레이어 피로도 증가 함수 호출
            PlayerStatus.Instance.AddFatigue(fatiguePerSec);
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
