using System;
using System.Collections;
using TMPro;
using UnityEngine;

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

        if (!HasEnoughMiniGamesForDay())
        {
            gameRoutine = null;
            yield break;
        }

        // Inspector registration order is the play order for each day.
        for (int index = 0; index < miniGamesPerDay; index++)
        {
            if (playerStatus != null && playerStatus.IsGameOver)
            {
                gameRoutine = null;
                yield break;
            }

            MiniGameData data = miniGames[index];

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

            yield return PlayMiniGameRoutine(data, balanceSettings);

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
        yield return PlayMiniGameRoutine(data, balanceSettings);

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
        GameBalanceSettings balanceSettings)
    {
        lastMiniGameResult = null;
        ClearTimerText();
        yield return new WaitForSeconds(
            balanceSettings.MiniGameReadyDuration);

        if (playerStatus == null || playerStatus.IsGameOver)
            yield break;

        if (!SpawnMiniGame(data))
            yield break;

        float timer = data.timeLimit;
        UpdateTimerText(timer);

        while (timer > 0f &&
               currentMiniGame != null &&
               currentMiniGame.IsPlaying)
        {
            float deltaTime = Time.deltaTime;
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

    private bool SpawnMiniGame(MiniGameData data)
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
        currentMiniGame.Init(data, CurrentDay);
        currentMiniGame.Play();

        return true;
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
        currentMiniGame.Stop();
        Destroy(currentMiniGame.gameObject);
        currentMiniGame = null;
    }
}
