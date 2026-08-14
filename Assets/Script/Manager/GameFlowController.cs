using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(MiniGameManager))]
public sealed class GameFlowController : MonoBehaviour
{
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private bool startNewGameOnStart = true;
    [SerializeField] private string outOfHealthEndingScene =
        "BadEnding_OutOfHp";
    [SerializeField] private string lowStudyEndingScene =
        "BadEnding_LackOfStudy";
    [SerializeField] private string goodEndingScene = "GoodEnding";

    private DaySystem daySystem;
    private PlayerStatus playerStatus;
    private bool endingTransitionRequested;

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
        {
            daySystem.OnDayStarted += HandleDayStarted;
            daySystem.OnFinalDayCompleted += HandleFinalDayCompleted;
        }

        if (playerStatus != null)
            playerStatus.GameOverTriggered += HandleGameOver;
    }

    private void Start()
    {
        if (!startNewGameOnStart || daySystem == null)
            return;

        StartNewGame();
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
        {
            daySystem.OnDayStarted -= HandleDayStarted;
            daySystem.OnFinalDayCompleted -= HandleFinalDayCompleted;
        }

        if (playerStatus != null)
            playerStatus.GameOverTriggered -= HandleGameOver;
    }

    public void StartNewGame()
    {
        if (playerStatus == null || daySystem == null)
            return;

        endingTransitionRequested = false;
        playerStatus.ResetStatus();
        miniGameManager.ResetInstructionHistory();
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

        if (reason == GameOverReason.HealthDepleted)
            LoadEndingScene(outOfHealthEndingScene);
    }

    private void HandleFinalDayCompleted(int completedDay)
    {
        if (daySystem == null ||
            playerStatus == null ||
            completedDay < daySystem.FinalDay)
        {
            return;
        }

        int targetStudyAmount = GetTargetStudyAmount();
        string endingScene = playerStatus.StudyAmount >= targetStudyAmount
            ? goodEndingScene
            : lowStudyEndingScene;

        Debug.Log(
            $"[GameFlowController] Final study amount: " +
            $"{playerStatus.StudyAmount}/{targetStudyAmount}. " +
            $"Loading {endingScene}.");
        LoadEndingScene(endingScene);
    }

    private int GetTargetStudyAmount()
    {
        GameBalanceSettings settings = playerStatus.Settings;

        return settings.StudyAmountPerMiniGame *
               settings.NormalMiniGamesPerDay *
               settings.FinalDay;
    }

    private void LoadEndingScene(string sceneName)
    {
        if (endingTransitionRequested || string.IsNullOrWhiteSpace(sceneName))
            return;

        endingTransitionRequested = true;
        miniGameManager.StopMiniGameFlow();
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(sceneName);
    }
}
