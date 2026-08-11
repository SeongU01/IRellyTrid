using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(MiniGameManager))]
public sealed class GameFlowController : MonoBehaviour
{
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private bool startNewGameOnStart = true;

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
            "[GameFlowController] Required game flow components are missing.");
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
            miniGameManager.OnMiniGameCompleted +=
                HandleMiniGameCompleted;
            miniGameManager.OnNormalMiniGamesCompleted +=
                HandleNormalMiniGamesCompleted;
        }

        if (daySystem != null)
            daySystem.OnDayStarted += HandleDayStarted;

        if (playerStatus != null)
            playerStatus.GameOverTriggered += HandleGameOver;
    }

    private void Start()
    {
        if (!startNewGameOnStart || daySystem == null)
            return;

        daySystem.StartNewGame();
        daySystem.StartCurrentDay();
    }

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.f7Key.wasPressedThisFrame ||
            daySystem == null)
        {
            return;
        }

        daySystem.AdvanceDayForDebug();
    }

    private void OnDisable()
    {
        if (miniGameManager != null)
        {
            miniGameManager.OnMiniGameCompleted -=
                HandleMiniGameCompleted;
            miniGameManager.OnNormalMiniGamesCompleted -=
                HandleNormalMiniGamesCompleted;
        }

        if (daySystem != null)
            daySystem.OnDayStarted -= HandleDayStarted;

        if (playerStatus != null)
            playerStatus.GameOverTriggered -= HandleGameOver;
    }

    public void StartNewGame()
    {
        if (playerStatus == null || daySystem == null)
            return;

        playerStatus.ResetStatus();
        daySystem.StartNewGame();
        daySystem.StartCurrentDay();
    }

    public void CompleteDay()
    {
        daySystem?.CompleteDay();
    }

    public void StartNextDay()
    {
        daySystem?.StartNextDay();
    }

    private void HandleDayStarted(int day)
    {
        miniGameManager.StartMiniGameFlow(day);
    }

    private void HandleMiniGameCompleted(MiniGameResult result)
    {
        if (!result.IsBonus)
            daySystem.RegisterNormalMiniGameCompleted();
    }

    private void HandleNormalMiniGamesCompleted(int day)
    {
        if (day != daySystem.CurrentDay)
        {
            Debug.LogWarning(
                $"[GameFlowController] Ignored completion for day {day}; " +
                $"current day is {daySystem.CurrentDay}.");
            return;
        }

        daySystem.BeginFirstBonusChoice();
    }

    private void HandleGameOver(GameOverReason reason)
    {
        daySystem.SetGameOver(reason);
    }
}
