using System;
using UnityEngine;

public sealed class DaySystem : MonoBehaviour
{
    private static DaySystem instance;

    private PlayerStatus playerStatus;
    private int studyAmountAtDayStart;
    private GameOverReason? lastGameOverReason;

    public static DaySystem Instance
    {
        get
        {
            if (instance == null && Application.isPlaying)
                CreateRuntimeInstance();

            return instance;
        }
    }

    public int CurrentDay { get; private set; } = 1;
    public DayPhase CurrentPhase { get; private set; } = DayPhase.NotStarted;
    public int CompletedNormalMiniGames { get; private set; }
    public int NormalMiniGamesPerDay =>
        playerStatus != null && playerStatus.Settings != null
            ? playerStatus.Settings.NormalMiniGamesPerDay
            : 0;
    public int FinalDay =>
        playerStatus != null && playerStatus.Settings != null
            ? playerStatus.Settings.FinalDay
            : 1;
    public bool IsFinalDay => CurrentDay >= FinalDay;
    public int TodayStudyAmount =>
        playerStatus == null
            ? 0
            : Mathf.Max(0, playerStatus.StudyAmount - studyAmountAtDayStart);
    public GameOverReason? LastGameOverReason => lastGameOverReason;

    public event Action<int> OnDayStarted;
    public event Action<int, int> OnDayChanged;
    public event Action<int, int> OnDayCompleted;
    public event Action<int> OnFinalDayCompleted;
    public event Action<DayPhase, DayPhase> OnPhaseChanged;
    public event Action<int, int> OnNormalMiniGameProgressChanged;
    public event Action OnNormalMiniGamesCompleted;
    public event Action<GameOverReason> OnGameOver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        CreateRuntimeInstance();
    }

    private static void CreateRuntimeInstance()
    {
        if (instance != null)
            return;

        GameObject daySystemObject = new GameObject(nameof(DaySystem));
        daySystemObject.AddComponent<DaySystem>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        playerStatus = PlayerStatus.Instance;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void StartNewGame()
    {
        int previousDay = CurrentDay;

        CurrentDay = 1;
        CompletedNormalMiniGames = 0;
        studyAmountAtDayStart =
            playerStatus != null ? playerStatus.StudyAmount : 0;
        lastGameOverReason = null;
        ChangePhase(DayPhase.NotStarted);

        if (previousDay != CurrentDay)
            OnDayChanged?.Invoke(previousDay, CurrentDay);

        OnNormalMiniGameProgressChanged?.Invoke(
            CompletedNormalMiniGames,
            NormalMiniGamesPerDay);

        Debug.Log(
            $"[DaySystem] New game initialized. Day {CurrentDay}/{FinalDay}");
    }

    public void StartCurrentDay()
    {
        if (!CanContinue())
            return;

        CompletedNormalMiniGames = 0;
        studyAmountAtDayStart = playerStatus.StudyAmount;
        ChangePhase(DayPhase.NormalMiniGames);
        OnNormalMiniGameProgressChanged?.Invoke(
            CompletedNormalMiniGames,
            NormalMiniGamesPerDay);

        Debug.Log(
            $"[DaySystem] Day {CurrentDay} started. " +
            $"Normal mini games: {NormalMiniGamesPerDay}");
        OnDayStarted?.Invoke(CurrentDay);
    }

    public void RegisterNormalMiniGameCompleted()
    {
        if (CurrentPhase != DayPhase.NormalMiniGames || !CanContinue())
            return;

        CompletedNormalMiniGames = Mathf.Min(
            CompletedNormalMiniGames + 1,
            NormalMiniGamesPerDay);

        Debug.Log(
            $"[DaySystem] Normal mini game progress: " +
            $"{CompletedNormalMiniGames}/{NormalMiniGamesPerDay}");
        OnNormalMiniGameProgressChanged?.Invoke(
            CompletedNormalMiniGames,
            NormalMiniGamesPerDay);
    }

    public void BeginFirstBonusChoice()
    {
        if (CurrentPhase != DayPhase.NormalMiniGames || !CanContinue())
            return;

        if (CompletedNormalMiniGames < NormalMiniGamesPerDay)
        {
            Debug.LogWarning(
                "[DaySystem] Bonus choice cannot begin before all normal " +
                "mini games are completed.");
            return;
        }

        ChangePhase(DayPhase.FirstBonusChoice);
        Debug.Log($"[DaySystem] Day {CurrentDay}: first bonus choice.");
        OnNormalMiniGamesCompleted?.Invoke();
    }

    public void BeginFirstBonusMiniGame()
    {
        ChangeActivePhase(
            DayPhase.FirstBonusChoice,
            DayPhase.FirstBonusMiniGame);
    }

    public void BeginSecondBonusChoice()
    {
        ChangeActivePhase(
            DayPhase.FirstBonusMiniGame,
            DayPhase.SecondBonusChoice);
    }

    public void BeginSecondBonusMiniGame()
    {
        ChangeActivePhase(
            DayPhase.SecondBonusChoice,
            DayPhase.SecondBonusMiniGame);
    }

    public void BeginRecovery()
    {
        if (!CanContinue())
            return;

        if (CurrentPhase != DayPhase.FirstBonusChoice &&
            CurrentPhase != DayPhase.FirstBonusMiniGame &&
            CurrentPhase != DayPhase.SecondBonusChoice &&
            CurrentPhase != DayPhase.SecondBonusMiniGame)
        {
            Debug.LogWarning(
                $"[DaySystem] Recovery cannot begin from {CurrentPhase}.");
            return;
        }

        ChangePhase(DayPhase.Recovery);
    }

    public void CompleteDay()
    {
        if (!CanContinue() ||
            CurrentPhase == DayPhase.DayCompleted ||
            CurrentPhase == DayPhase.Ending)
        {
            return;
        }

        ChangePhase(DayPhase.DayCompleted);

        int completedDay = CurrentDay;
        int todayStudyAmount = TodayStudyAmount;
        Debug.Log(
            $"[DaySystem] Day {completedDay} completed. " +
            $"Today's study amount: {todayStudyAmount}");
        OnDayCompleted?.Invoke(completedDay, todayStudyAmount);

        if (!IsFinalDay)
            return;

        ChangePhase(DayPhase.Ending);
        Debug.Log(
            $"[DaySystem] Final day {completedDay} completed. " +
            $"Total study amount: {playerStatus.StudyAmount}");
        OnFinalDayCompleted?.Invoke(completedDay);
    }

    public void StartNextDay()
    {
        if (CurrentPhase != DayPhase.DayCompleted || !CanContinue())
            return;

        if (IsFinalDay)
        {
            Debug.LogWarning(
                "[DaySystem] The final day is complete. " +
                "A next day cannot be started.");
            return;
        }

        int previousDay = CurrentDay;
        CurrentDay++;
        OnDayChanged?.Invoke(previousDay, CurrentDay);
        StartCurrentDay();
    }

    public void SetGameOver(GameOverReason reason)
    {
        if (CurrentPhase == DayPhase.GameOver)
            return;

        lastGameOverReason = reason;
        ChangePhase(DayPhase.GameOver);
        Debug.Log(
            $"[DaySystem] Game over on day {CurrentDay}: {reason}");
        OnGameOver?.Invoke(reason);
    }

    private bool CanContinue()
    {
        if (playerStatus != null && !playerStatus.IsGameOver)
            return true;

        if (playerStatus != null && playerStatus.IsGameOver &&
            CurrentPhase != DayPhase.GameOver)
        {
            Debug.LogWarning(
                "[DaySystem] Day flow cannot continue while the game is over.");
        }

        return false;
    }

    private void ChangeActivePhase(
        DayPhase requiredPhase,
        DayPhase nextPhase)
    {
        if (!CanContinue())
            return;

        if (CurrentPhase != requiredPhase)
        {
            Debug.LogWarning(
                $"[DaySystem] {nextPhase} cannot begin from {CurrentPhase}.");
            return;
        }

        ChangePhase(nextPhase);
    }

    private void ChangePhase(DayPhase nextPhase)
    {
        if (CurrentPhase == nextPhase)
            return;

        DayPhase previousPhase = CurrentPhase;
        CurrentPhase = nextPhase;
        Debug.Log(
            $"[DaySystem] Phase: {previousPhase} -> {CurrentPhase}");
        OnPhaseChanged?.Invoke(previousPhase, CurrentPhase);
    }
}
