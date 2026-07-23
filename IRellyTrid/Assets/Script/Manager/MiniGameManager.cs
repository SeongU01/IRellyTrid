using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class MiniGameManager : MonoBehaviour
{
    [Header("MiniGame Settings")]
    [SerializeField] private Transform miniGameRoot;
    [SerializeField] private MiniGameData[] miniGames;

    [Header("Flow Settings")]
    [Min(1)]
    [SerializeField] private int miniGamesPerDay = 5;
    [SerializeField] private float readyTime = 1f;
    [SerializeField] private float resultTime = 1f;

    [Header("Common UI")]
    [SerializeField] private TMP_Text timerText;

    private MiniGameBase currentMiniGame;
    private Coroutine gameRoutine;

    public event Action<int> OnDayCompleted;

    public int CurrentDay { get; private set; } = 1;

    private void Start()
    {
        StartMiniGameFlow();
    }

    public void StartMiniGameFlow()
    {
        StartMiniGameFlow(CurrentDay);
    }

    public void StartMiniGameFlow(int day)
    {
        if (gameRoutine != null)
        {
            StopCoroutine(gameRoutine);
            gameRoutine = null;
        }

        DestroyCurrentMiniGame();
        CurrentDay = Mathf.Max(1, day);
        gameRoutine = StartCoroutine(GameFlowRoutine());
    }

    public void StartNextDay()
    {
        StartMiniGameFlow(CurrentDay + 1);
    }

    private IEnumerator GameFlowRoutine()
    {
        if (!HasEnoughMiniGamesForDay())
        {
            gameRoutine = null;
            yield break;
        }

        // Inspector registration order is the play order for each day.
        for (int index = 0; index < miniGamesPerDay; index++)
        {
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

            ClearTimerText();
            yield return new WaitForSeconds(readyTime);

            if (!SpawnMiniGame(data))
            {
                gameRoutine = null;
                yield break;
            }

            float timer = data.timeLimit;
            UpdateTimerText(timer);

            while (timer > 0f && currentMiniGame != null && currentMiniGame.IsPlaying)
            {
                timer -= Time.deltaTime;
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

            yield return new WaitForSeconds(resultTime);
        }

        gameRoutine = null;
        OnDayCompleted?.Invoke(CurrentDay);
    }

    private bool HasEnoughMiniGamesForDay()
    {
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
        Debug.Log(result.Success ? "Success" : "Failure");
#endif
        DestroyCurrentMiniGame();
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
