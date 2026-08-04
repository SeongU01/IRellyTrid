using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class MiniGameManager : MonoBehaviour
{
    [Header("MiniGame Settings")]
    [SerializeField] private Transform miniGameRoot;
    [SerializeField] private MiniGameData[] miniGames;

    [Header("Common UI")]
    [SerializeField] private TMP_Text timerText;

    private MiniGameBase currentMiniGame;
    private Coroutine gameRoutine;
    private PlayerStatus playerStatus;
    private MiniGameResult lastMiniGameResult;
    private bool timerResetRequested;

    public event Action<int> OnNormalMiniGamesCompleted;
    public event Action<MiniGameResult> OnMiniGameCompleted;
    public event Action<MiniGameResult> OnBonusMiniGameCompleted;

    public int CurrentDay { get; private set; } = 1;

    private void OnEnable()
    {
        playerStatus = PlayerStatus.Instance;

        if (playerStatus != null)
            playerStatus.GameOverTriggered += HandleGameOver;
    }

    private void OnDisable()
    {
        if (playerStatus != null)
            playerStatus.GameOverTriggered -= HandleGameOver;
    }

    public void StartMiniGameFlow()
    {
        StartMiniGameFlow(CurrentDay);
    }

    public void StartMiniGameFlow(int day)
    {
        if (playerStatus != null && playerStatus.IsGameOver)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Mini game flow cannot start while the game is over.");
#endif
            return;
        }

        if (gameRoutine != null)
        {
            StopCoroutine(gameRoutine);
            gameRoutine = null;
        }

        DestroyCurrentMiniGame();
        CurrentDay = Mathf.Max(1, day);
        gameRoutine = StartCoroutine(GameFlowRoutine());
    }

    public bool StartBonusMiniGame(MiniGameData data, int day)
    {
        if (playerStatus != null && playerStatus.IsGameOver)
        {
#if UNITY_EDITOR
            Debug.LogWarning(
                "A bonus mini game cannot start while the game is over.");
#endif
            return false;
        }

        if (data == null || data.prefab == null)
        {
#if UNITY_EDITOR
            Debug.LogError(
                "The bonus mini game is not configured correctly.");
#endif
            return false;
        }

        if (!data.isBonus)
        {
#if UNITY_EDITOR
            Debug.LogError(
                $"{data.name} is not marked as a bonus mini game.");
#endif
            return false;
        }

        StopMiniGameFlow();
        CurrentDay = Mathf.Max(1, day);
        gameRoutine = StartCoroutine(BonusGameRoutine(data));
        return true;
    }

    private IEnumerator GameFlowRoutine()
    {
        if (playerStatus != null && playerStatus.IsGameOver)
        {
            gameRoutine = null;
            yield break;
        }

        GameBalanceSettings balanceSettings = playerStatus.Settings;
        int miniGamesPerDay = balanceSettings.NormalMiniGamesPerDay;
        GameDaySettings daySettings =
            balanceSettings.GetDaySettings(CurrentDay);

        if (!HasEnoughMiniGamesForDay())
        {
            gameRoutine = null;
            yield break;
        }

        List<MiniGameData> gamesForToday =
            CreateGamesForToday(miniGamesPerDay, daySettings);

        float effectiveTimeLimit = daySettings != null
            ? daySettings.MiniGameTimeLimit
            : 30f;

        int difficultyDay = daySettings != null &&
            daySettings.MiniGameDifficultyDay > 0
                ? daySettings.MiniGameDifficultyDay
                : CurrentDay;

#if UNITY_EDITOR
        Debug.Log(
            $"[MiniGameManager] Day {CurrentDay}: " +
            $"order={(daySettings != null && daySettings.RandomizeMiniGameOrder ? "random" : "fixed")}, " +
            $"time={effectiveTimeLimit:0.##}s, difficulty day={difficultyDay}.");
#endif

        for (int index = 0; index < miniGamesPerDay; index++)
        {
            if (playerStatus != null && playerStatus.IsGameOver)
            {
                gameRoutine = null;
                yield break;
            }

            MiniGameData data = gamesForToday[index];

            if (data == null || data.prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"Mini game slot {index + 1} is not configured correctly.");
#endif
                gameRoutine = null;
                yield break;
            }

#if UNITY_EDITOR
            Debug.Log($"Command: {data.commandText}");
#endif

            yield return PlayMiniGameRoutine(
                data,
                balanceSettings,
                effectiveTimeLimit,
                difficultyDay);

            if (lastMiniGameResult == null)
            {
                gameRoutine = null;
                yield break;
            }
        }

        gameRoutine = null;

        if (playerStatus == null || !playerStatus.IsGameOver)
            OnNormalMiniGamesCompleted?.Invoke(CurrentDay);
    }

    private IEnumerator BonusGameRoutine(MiniGameData data)
    {
        GameBalanceSettings balanceSettings = playerStatus.Settings;
        yield return PlayMiniGameRoutine(
            data,
            balanceSettings,
            data.timeLimit,
            CurrentDay);

        MiniGameResult completedResult = lastMiniGameResult;
        gameRoutine = null;

        if (completedResult != null &&
            playerStatus != null &&
            !playerStatus.IsGameOver)
        {
            OnBonusMiniGameCompleted?.Invoke(completedResult);
        }
    }

    private IEnumerator PlayMiniGameRoutine(
        MiniGameData data,
        GameBalanceSettings balanceSettings,
        float effectiveTimeLimit,
        int difficultyDay)
    {
        lastMiniGameResult = null;
        ClearTimerText();
        yield return new WaitForSeconds(
            balanceSettings.MiniGameReadyDuration);

        if (playerStatus == null || playerStatus.IsGameOver)
            yield break;

        if (!SpawnMiniGame(
                data,
                difficultyDay,
                effectiveTimeLimit))
            yield break;

        float timer = effectiveTimeLimit;
        timerResetRequested = false;
        UpdateTimerText(timer);

        while (timer > 0f &&
               currentMiniGame != null &&
               currentMiniGame.IsPlaying)
        {
#if UNITY_EDITOR
            // TODO: 나중에 삭제해야 하는 Unity Editor 전용 테스트 코드.
            if (Keyboard.current != null &&
                Keyboard.current.f8Key.wasPressedThisFrame)
            {
                Debug.Log(
                    "[MiniGameManager] F8 pressed: current mini game " +
                    "completed as success for editor testing.");
                currentMiniGame.CompleteAsSuccessForEditorTest();
                continue;
            }
#endif

            float deltaTime = Time.deltaTime;

            if (timerResetRequested)
            {
                timer = effectiveTimeLimit;
                timerResetRequested = false;
                UpdateTimerText(timer);
                yield return null;
                continue;
            }

            timer -= deltaTime;

            if (playerStatus != null)
            {
                playerStatus.AddFatigue(
                    deltaTime * playerStatus.Settings.FatiguePerSecond);
            }

            UpdateTimerText(timer);
            yield return null;
        }

        ClearTimerText();

        if (currentMiniGame != null && currentMiniGame.IsPlaying)
        {
#if UNITY_EDITOR
            Debug.Log("Time out");
#endif
            currentMiniGame.Timeout();
        }

        if (playerStatus == null || playerStatus.IsGameOver)
            yield break;

        yield return new WaitForSeconds(
            balanceSettings.MiniGameResultDuration);
    }

    private List<MiniGameData> CreateGamesForToday(
        int miniGamesPerDay,
        GameDaySettings daySettings)
    {
        List<MiniGameData> result = new List<MiniGameData>(miniGamesPerDay);

        for (int i = 0; i < miniGamesPerDay; i++)
        {
            MiniGameData overrideData =
                daySettings?.GetMiniGameOverride(i);
            result.Add(overrideData ?? miniGames[i]);
        }

        if (daySettings == null || !daySettings.RandomizeMiniGameOrder)
            return result;

        for (int i = result.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (result[i], result[randomIndex]) =
                (result[randomIndex], result[i]);
        }

        return result;
    }

    private bool HasEnoughMiniGamesForDay()
    {
        int miniGamesPerDay =
            playerStatus.Settings.NormalMiniGamesPerDay;

        if (miniGames != null && miniGames.Length >= miniGamesPerDay)
            return true;

#if UNITY_EDITOR
        int registeredCount = miniGames == null ? 0 : miniGames.Length;
        Debug.LogError(
            $"A day requires {miniGamesPerDay} mini games, but only " +
            $"{registeredCount} are registered.");
#endif
        return false;
    }

    private void ClearTimerText()
    {
        if (timerText != null)
            timerText.text = string.Empty;
    }

    private void UpdateTimerText(float time)
    {
        if (timerText == null)
            return;

        timerText.text = Mathf.CeilToInt(Mathf.Max(0f, time)).ToString();
    }

    private bool SpawnMiniGame(
        MiniGameData data,
        int difficultyDay,
        float effectiveTimeLimit)
    {
        if (data.prefab == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Mini game prefab is missing.");
#endif
            return false;
        }

        currentMiniGame = Instantiate(data.prefab, miniGameRoot);
        currentMiniGame.OnFinished += HandleMiniGameFinished;
        currentMiniGame.OnTimerResetRequested += HandleTimerResetRequested;
        currentMiniGame.Init(
            data,
            CurrentDay,
            difficultyDay,
            effectiveTimeLimit);
        currentMiniGame.Play();

        return true;
    }

    private void HandleTimerResetRequested()
    {
        timerResetRequested = true;
    }

    private void HandleMiniGameFinished(MiniGameResult result)
    {
#if UNITY_EDITOR
        Debug.Log(
            $"{result.MiniGameName} finished: {result.EndReason}, " +
            $"{result.PlayDuration:0.00}s");
#endif

        ApplyMiniGameResult(result);
        lastMiniGameResult = result;
        OnMiniGameCompleted?.Invoke(result);
        DestroyCurrentMiniGame();
    }

    private void ApplyMiniGameResult(MiniGameResult result)
    {
        if (playerStatus == null || playerStatus.IsGameOver)
            return;

        if (result.Success)
        {
            playerStatus.AddStudyAmount(
                playerStatus.Settings.StudyAmountPerMiniGame);
            return;
        }

        if (result.EndReason == MiniGameEndReason.Failure)
        {
            playerStatus.TakeDamage(
                playerStatus.Settings.FailureHealthDamage);
            return;
        }

        if (result.EndReason == MiniGameEndReason.Timeout)
        {
            playerStatus.TakeDamage(
                playerStatus.Settings.TimeoutHealthDamage);
        }
    }

    private void HandleGameOver(GameOverReason reason)
    {
#if UNITY_EDITOR
        Debug.Log($"Mini game flow stopped: {reason}");
#endif

        StopMiniGameFlow();
    }

    public void StopMiniGameFlow()
    {
        if (gameRoutine != null)
        {
            StopCoroutine(gameRoutine);
            gameRoutine = null;
        }

        ClearTimerText();
        DestroyCurrentMiniGame();
        lastMiniGameResult = null;
    }

    private void DestroyCurrentMiniGame()
    {
        if (currentMiniGame == null)
            return;

        currentMiniGame.OnFinished -= HandleMiniGameFinished;
        currentMiniGame.OnTimerResetRequested -= HandleTimerResetRequested;
        currentMiniGame.Stop();
        Destroy(currentMiniGame.gameObject);
        currentMiniGame = null;
        timerResetRequested = false;
    }
}
