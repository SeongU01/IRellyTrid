using UnityEngine;

/// <summary>
/// PlayerStatus와 DaySystem 사용 예시
///
/// 플레이어 상태값은 직접 대입하지 않고 다음 함수를 사용합니다.
///
/// 체력 감소: PlayerStatus.Instance.TakeDamage(1);
/// 체력 회복: PlayerStatus.Instance.Heal(1);
/// 피로도 증가: PlayerStatus.Instance.AddFatigue(10f);
/// 피로도 회복: PlayerStatus.Instance.RecoverFatigue(20f);
/// 공부량 증가: PlayerStatus.Instance.AddStudyAmount(5);
/// </summary>
public class PlayerStatusUsage : MonoBehaviour
{
    private PlayerStatus playerStatus;
    private DaySystem daySystem;

    private void OnEnable()
    {
        // 플레이 중 인스턴스가 없으면 자동으로 생성됩니다.
        playerStatus = PlayerStatus.Instance;
        daySystem = DaySystem.Instance;

        // 상태 변경 이벤트 구독
        playerStatus.HealthChanged += HandleHealthChanged;
        playerStatus.FatigueChanged += HandleFatigueChanged;
        playerStatus.StudyAmountChanged += HandleStudyAmountChanged;
        playerStatus.GameOverTriggered += HandleGameOver;

        // 일차 관련 이벤트 구독
        daySystem.OnDayChanged += HandleDayChanged;
        daySystem.OnPhaseChanged += HandlePhaseChanged;

        // UI가 처음 활성화됐을 때 현재 값으로 갱신
        HandleHealthChanged(
            playerStatus.Health,
            playerStatus.MaxHealth);

        HandleFatigueChanged(
            playerStatus.Fatigue,
            playerStatus.MaxFatigue);

        HandleStudyAmountChanged(playerStatus.StudyAmount);
    }

    private void Start()
    {
        ReadCurrentPlayerStatus();
        ReadCurrentDayStatus();
        ReadBalanceSettings();
    }

    private void OnDisable()
    {
        // 이벤트 중복 호출과 메모리 문제를 막기 위해 반드시 구독 해제
        if (playerStatus != null)
        {
            playerStatus.HealthChanged -= HandleHealthChanged;
            playerStatus.FatigueChanged -= HandleFatigueChanged;
            playerStatus.StudyAmountChanged -= HandleStudyAmountChanged;
            playerStatus.GameOverTriggered -= HandleGameOver;
        }

        if (daySystem != null)
        {
            daySystem.OnDayChanged -= HandleDayChanged;
            daySystem.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    /// <summary>
    /// 현재 플레이어 상태값 가져오기
    /// </summary>
    private void ReadCurrentPlayerStatus()
    {
        int currentHealth = playerStatus.Health;
        int maxHealth = playerStatus.MaxHealth;

        float currentFatigue = playerStatus.Fatigue;
        float maxFatigue = playerStatus.MaxFatigue;

        int studyAmount = playerStatus.StudyAmount;
        bool isGameOver = playerStatus.IsGameOver;

        Debug.Log(
            $"Health: {currentHealth}/{maxHealth}, " +
            $"Fatigue: {currentFatigue:0.##}/{maxFatigue:0.##}, " +
            $"Study Amount: {studyAmount}, " +
            $"Game Over: {isGameOver}");
    }

    /// <summary>
    /// 현재 날짜 및 게임 진행 상태 가져오기
    /// </summary>
    private void ReadCurrentDayStatus()
    {
        int currentDay = daySystem.CurrentDay;
        DayPhase currentPhase = daySystem.CurrentPhase;
        int normalMiniGameProgress =
            daySystem.CompletedNormalMiniGames;
        int performedBonusMiniGames =
            daySystem.PerformedBonusMiniGames;

        Debug.Log(
            $"Day: {currentDay}, " +
            $"Phase: {currentPhase}, " +
            $"Normal Progress: {normalMiniGameProgress}, " +
            $"Bonus Count: {performedBonusMiniGames}");
    }

    /// <summary>
    /// Inspector에서 설정한 밸런스값 가져오기
    /// </summary>
    private void ReadBalanceSettings()
    {
        GameBalanceSettings settings = playerStatus.Settings;

        int maxHealth = settings.MaxHealth;
        float maxFatigue = settings.MaxFatigue;

        int studyReward = settings.StudyAmountPerMiniGame;
        float fatiguePerSecond = settings.FatiguePerSecond;

        float sleepRecovery = settings.SleepRecoveryAmount;
        float oneBonusRecovery = settings.OneBonusRecoveryAmount;
        float twoBonusRecovery = settings.TwoBonusRecoveryAmount;

        Debug.Log(
            $"Max Health: {maxHealth}, " +
            $"Max Fatigue: {maxFatigue}, " +
            $"Study Reward: {studyReward}, " +
            $"Fatigue Per Second: {fatiguePerSecond}, " +
            $"Recovery: {sleepRecovery}/{oneBonusRecovery}/" +
            $"{twoBonusRecovery}");
    }

    //────────────────────────────────────────────
    // 플레이어 상태 변경 방법
    //────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        playerStatus.TakeDamage(amount);
    }

    public void Heal(int amount)
    {
        playerStatus.Heal(amount);
    }

    public void AddFatigue(float amount)
    {
        playerStatus.AddFatigue(amount);
    }

    public void RecoverFatigue(float amount)
    {
        playerStatus.RecoverFatigue(amount);
    }

    public void AddStudyAmount(int amount)
    {
        playerStatus.AddStudyAmount(amount);
    }

    //────────────────────────────────────────────
    // 플레이어 상태 이벤트
    //────────────────────────────────────────────

    private void HandleHealthChanged(int current, int max)
    {
        Debug.Log($"Health Changed: {current}/{max}");

        // 체력 UI 갱신 코드를 여기에 작성
    }

    private void HandleFatigueChanged(float current, float max)
    {
        Debug.Log(
            $"Fatigue Changed: {current:0.##}/{max:0.##}");

        // 피로도 UI 갱신 코드를 여기에 작성
    }

    private void HandleStudyAmountChanged(int amount)
    {
        Debug.Log($"Study Amount Changed: {amount}");

        // 공부량 UI 갱신 코드를 여기에 작성
    }

    private void HandleGameOver(GameOverReason reason)
    {
        Debug.Log($"Game Over: {reason}");

        // 게임 오버 UI 표시 코드를 여기에 작성
    }

    //────────────────────────────────────────────
    // 일차 시스템 이벤트
    //────────────────────────────────────────────

    private void HandleDayChanged(int previousDay, int currentDay)
    {
        Debug.Log(
            $"Day Changed: {previousDay} -> {currentDay}");

        // 날짜 UI 갱신 코드를 여기에 작성
    }

    private void HandlePhaseChanged(
        DayPhase previousPhase,
        DayPhase currentPhase)
    {
        Debug.Log(
            $"Phase Changed: {previousPhase} -> {currentPhase}");

        // 현재 게임 진행 단계에 따른 UI 처리를 여기에 작성
    }
}