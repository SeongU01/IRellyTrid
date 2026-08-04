using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MiniGameManager))]
public sealed class BonusGameFlowController : MonoBehaviour
{
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField]
    private BonusMiniGameDayConfiguration[] bonusConfigurations;

    private DaySystem daySystem;
    private PlayerStatus playerStatus;

    private void Reset()
    {
        miniGameManager = GetComponent<MiniGameManager>();
    }

    private void Awake()
    {
        if (miniGameManager == null)
            miniGameManager = GetComponent<MiniGameManager>();

        daySystem = DaySystem.Instance;
        playerStatus = PlayerStatus.Instance;

        if (miniGameManager != null && daySystem != null &&
            playerStatus != null)
        {
            return;
        }

        Debug.LogError(
            "[BonusGameFlowController] Required game flow components " +
            "are missing.");
        enabled = false;
    }

    private void OnEnable()
    {
        if (miniGameManager == null)
            miniGameManager = GetComponent<MiniGameManager>();

        daySystem = DaySystem.Instance;
        playerStatus = PlayerStatus.Instance;

        if (miniGameManager != null)
        {
            miniGameManager.OnBonusMiniGameCompleted +=
                HandleBonusMiniGameCompleted;
        }

        if (playerStatus != null)
            playerStatus.GameOverTriggered += HandleGameOver;
    }

    private void OnDisable()
    {
        if (miniGameManager != null)
        {
            miniGameManager.OnBonusMiniGameCompleted -=
                HandleBonusMiniGameCompleted;
        }

        if (playerStatus != null)
            playerStatus.GameOverTriggered -= HandleGameOver;
    }

    private void OnValidate()
    {
        if (miniGameManager == null)
            miniGameManager = GetComponent<MiniGameManager>();

        if (bonusConfigurations == null)
            return;

        foreach (BonusMiniGameDayConfiguration configuration
                 in bonusConfigurations)
        {
            configuration?.EnsureBonusFlags();
        }
    }

    public void PlayFirstBonus()
    {
        if (!IsExpectedPhase(DayPhase.FirstBonusChoice))
            return;

        MiniGameData bonusData = GetBonusData(firstBonus: true);

        if (!IsConfigured(bonusData, "first"))
            return;

        daySystem.BeginFirstBonusMiniGame();

        if (daySystem.CurrentPhase != DayPhase.FirstBonusMiniGame)
            return;

        miniGameManager.StartBonusMiniGame(
            bonusData,
            daySystem.CurrentDay);
    }

    public void SkipFirstBonus()
    {
        if (!IsExpectedPhase(DayPhase.FirstBonusChoice))
            return;

        Debug.Log(
            $"[BonusGameFlowController] Day {daySystem.CurrentDay}: " +
            "first bonus skipped. The second bonus is also skipped.");
        daySystem.BeginRecovery();
    }

    public void PlaySecondBonus()
    {
        if (!IsExpectedPhase(DayPhase.SecondBonusChoice))
            return;

        MiniGameData bonusData = GetBonusData(firstBonus: false);

        if (!IsConfigured(bonusData, "second"))
            return;

        daySystem.BeginSecondBonusMiniGame();

        if (daySystem.CurrentPhase != DayPhase.SecondBonusMiniGame)
            return;

        miniGameManager.StartBonusMiniGame(
            bonusData,
            daySystem.CurrentDay);
    }

    public void SkipSecondBonus()
    {
        if (!IsExpectedPhase(DayPhase.SecondBonusChoice))
            return;

        Debug.Log(
            $"[BonusGameFlowController] Day {daySystem.CurrentDay}: " +
            "second bonus skipped.");
        daySystem.BeginRecovery();
    }

    private void HandleBonusMiniGameCompleted(MiniGameResult result)
    {
        if (result == null || !result.IsBonus ||
            result.Day != daySystem.CurrentDay ||
            playerStatus.IsGameOver)
        {
            return;
        }

        if (daySystem.CurrentPhase == DayPhase.FirstBonusMiniGame)
        {
            daySystem.RegisterBonusMiniGamePerformed();
            daySystem.BeginSecondBonusChoice();
            return;
        }

        if (daySystem.CurrentPhase == DayPhase.SecondBonusMiniGame)
        {
            daySystem.RegisterBonusMiniGamePerformed();
            daySystem.BeginRecovery();
        }
    }

    private void HandleGameOver(GameOverReason reason)
    {
        miniGameManager.StopMiniGameFlow();
    }

    private bool IsExpectedPhase(DayPhase expectedPhase)
    {
        if (daySystem != null && playerStatus != null &&
            !playerStatus.IsGameOver &&
            daySystem.CurrentPhase == expectedPhase)
        {
            return true;
        }

        DayPhase currentPhase =
            daySystem != null ? daySystem.CurrentPhase : DayPhase.NotStarted;
        Debug.LogWarning(
            $"[BonusGameFlowController] {expectedPhase} action ignored " +
            $"while the current phase is {currentPhase}.");
        return false;
    }

    private MiniGameData GetBonusData(bool firstBonus)
    {
        BonusMiniGameDayConfiguration configuration =
            FindConfiguration(daySystem.CurrentDay);

        if (configuration == null)
        {
            Debug.LogError(
                $"[BonusGameFlowController] No bonus configuration " +
                $"covers day {daySystem.CurrentDay}.");
            return null;
        }

        configuration.EnsureBonusFlags();
        return firstBonus
            ? configuration.FirstBonus
            : configuration.SecondBonus;
    }

    private BonusMiniGameDayConfiguration FindConfiguration(int day)
    {
        if (bonusConfigurations == null)
            return null;

        BonusMiniGameDayConfiguration selected = null;

        foreach (BonusMiniGameDayConfiguration configuration
                 in bonusConfigurations)
        {
            if (configuration == null || configuration.StartDay > day)
                continue;

            if (selected == null ||
                configuration.StartDay > selected.StartDay)
            {
                selected = configuration;
            }
        }

        return selected;
    }

    private static bool IsConfigured(
        MiniGameData data,
        string bonusOrder)
    {
        if (data != null && data.prefab != null)
            return true;

        Debug.LogError(
            $"[BonusGameFlowController] The {bonusOrder} bonus mini game " +
            "is not configured.");
        return false;
    }
}
