using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public class MiniGameManager : MonoBehaviour
{
    [Header("MiniGame Settings")]
    [SerializeField] private Transform miniGameRoot;
    [SerializeField] private MiniGameData[] miniGames;

    [Header("Common UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Day Background")]
    [SerializeField] private SpriteRenderer worldBackgroundRenderer;
    [SerializeField] private Sprite dayOneToFiveBackground;
    [SerializeField] private Sprite daySixPlusBackground;

    [Header("Day Transition")]
    [SerializeField, Min(1f)] private float dayTransitionFontSize = 120f;
    [SerializeField] private int dayTransitionSortingOrder = 1000;
    [SerializeField, Min(0f)] private float screenFadeDuration = 0.4f;
    [SerializeField, Min(0f)] private float dayTextFadeInDuration = 0.4f;
    [SerializeField, Min(0f)] private float dayTextDotInterval = 0.35f;
    [SerializeField, Min(0f)] private float dayTextHoldDuration = 0.7f;
    [SerializeField, Min(0f)] private float dayTextFadeOutDuration = 0.4f;

    [Header("Mini Game Instruction")]
    [SerializeField, Min(1f)] private float instructionFontSize = 72f;
    [SerializeField] private Vector2 instructionPosition =
        new Vector2(-165f, -150f);
    [SerializeField] private int instructionSortingOrder = 900;
    [SerializeField, Min(0f)] private float instructionFadeInDuration = 0.3f;
    [SerializeField, Min(0f)] private float instructionHoldDuration = 0.6f;
    [SerializeField, Min(0f)] private float instructionFadeOutDuration = 0.3f;

    private MiniGameBase currentMiniGame;
    private Coroutine gameRoutine;
    private PlayerStatus playerStatus;
    private MiniGameResult lastMiniGameResult;
    private bool timerResetRequested;
    private GameObject dayTransitionRoot;
    private TMP_Text dayTransitionText;
    private CanvasGroupFader screenFader;
    private CanvasGroupFader dayTextFader;
    private GameObject instructionRoot;
    private TMP_Text instructionText;
    private CanvasGroupFader instructionFader;
    private bool firstDayTransitionCompleted;
    private bool startDayTransitionCovered;
    private bool isPaused;
    private bool currentMiniGameWasEnabledBeforePause;
    private bool currentMiniGameIsBonus;
    private float timeScaleBeforePause = 1f;
    private readonly HashSet<string> shownInstructions =
        new HashSet<string>(StringComparer.Ordinal);

    public event Action<int> OnNormalMiniGamesCompleted;
    public event Action<MiniGameResult> OnMiniGameCompleted;
    public event Action<MiniGameResult> OnBonusMiniGameCompleted;

    public int CurrentDay { get; private set; } = 1;
    public bool IsPaused => isPaused;
    public bool IsMiniGamePlaying =>
        !isPaused && currentMiniGame != null && currentMiniGame.IsPlaying;
    public bool IsBonusMiniGamePlaying =>
        IsMiniGamePlaying && currentMiniGameIsBonus;
    public bool IsDayTransitionCovered =>
        dayTransitionRoot != null &&
        dayTransitionRoot.activeInHierarchy &&
        screenFader != null &&
        screenFader.Alpha >= 1f;

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

        if (isPaused)
            SetPaused(false);
    }

    public void SetPaused(bool paused)
    {
        if (isPaused == paused)
            return;

        isPaused = paused;

        if (paused)
        {
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = timeScaleBeforePause;
        }

        if (currentMiniGame == null)
            return;

        if (paused)
        {
            currentMiniGameWasEnabledBeforePause =
                currentMiniGame.enabled;
            currentMiniGame.enabled = false;
            return;
        }

        currentMiniGame.enabled = currentMiniGameWasEnabledBeforePause;
    }

    public void StartMiniGameFlow()
    {
        StartMiniGameFlow(CurrentDay);
    }

    public void ResetInstructionHistory()
    {
        shownInstructions.Clear();
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
        startDayTransitionCovered =
            CurrentDay == 1 && !firstDayTransitionCompleted;

        if (startDayTransitionCovered)
            PrepareDayTransitionCovered();

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

        yield return PlayDayTransitionRoutine();

        if (playerStatus == null || playerStatus.IsGameOver)
        {
            gameRoutine = null;
            yield break;
        }

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

        yield return PlayInstructionRoutine(data);

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
            if (Keyboard.current != null &&
                Keyboard.current.f8Key.wasPressedThisFrame)
            {
                Debug.Log(
                    "[MiniGameManager] F8 pressed: current mini game " +
                    "completed as success by debug shortcut.");
                currentMiniGame.CompleteAsSuccessForDebug();
                continue;
            }

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

    private IEnumerator PlayInstructionRoutine(MiniGameData data)
    {
        if (data == null ||
            string.IsNullOrWhiteSpace(data.name) ||
            string.IsNullOrWhiteSpace(data.commandText) ||
            shownInstructions.Contains(data.name) ||
            !EnsureInstructionUI())
        {
            yield break;
        }

        shownInstructions.Add(data.name);
        instructionRoot.SetActive(true);
        instructionRoot.transform.SetAsLastSibling();
        instructionText.text = data.commandText;
        instructionFader.SetAlpha(0f);

        Coroutine instructionSequence = instructionFader.PlayFadeInOut(
            instructionFadeInDuration,
            instructionHoldDuration,
            instructionFadeOutDuration);

        if (instructionSequence != null)
            yield return instructionSequence;

        HideInstructionImmediately();
    }

    private bool EnsureInstructionUI()
    {
        if (instructionRoot != null)
            return true;

        if (timerText == null || timerText.transform.parent == null)
        {
            Debug.LogWarning(
                "[MiniGameManager] Instruction UI requires the common UI canvas.");
            return false;
        }

        Transform canvasTransform = timerText.transform.parent;
        int uiLayer = timerText.gameObject.layer;

        instructionRoot = new GameObject(
            "MiniGameInstruction",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(CanvasGroupFader));
        instructionRoot.layer = uiLayer;
        RectTransform rootRect =
            instructionRoot.GetComponent<RectTransform>();
        rootRect.SetParent(canvasTransform, false);
        StretchToParent(rootRect);

        Canvas instructionCanvas = instructionRoot.GetComponent<Canvas>();
        instructionCanvas.overrideSorting = true;
        instructionCanvas.sortingOrder = instructionSortingOrder;

        GameObject blockerObject = new GameObject(
            "InputBlocker",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        blockerObject.layer = uiLayer;
        RectTransform blockerRect =
            blockerObject.GetComponent<RectTransform>();
        blockerRect.SetParent(rootRect, false);
        StretchToParent(blockerRect);
        Image blockerImage = blockerObject.GetComponent<Image>();
        blockerImage.color = Color.clear;
        blockerImage.raycastTarget = true;

        GameObject textObject = new GameObject(
            "InstructionText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = uiLayer;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(rootRect, false);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = instructionPosition;
        textRect.sizeDelta = new Vector2(1500f, 240f);

        instructionText = textObject.GetComponent<TMP_Text>();
        instructionText.font = KoreanFontBootstrap.FontAsset ?? timerText.font;
        instructionText.fontSize = instructionFontSize;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.color = Color.black;
        instructionText.raycastTarget = false;

        instructionFader =
            instructionRoot.GetComponent<CanvasGroupFader>();
        instructionFader.Configure(true, true, true);
        instructionRoot.SetActive(false);
        return true;
    }

    private void HideInstructionImmediately()
    {
        instructionFader?.SetAlpha(0f);

        if (instructionRoot != null)
            instructionRoot.SetActive(false);
    }

    private IEnumerator PlayDayTransitionRoutine()
    {
        if (!EnsureDayTransitionUI())
            yield break;

        dayTransitionRoot.SetActive(true);
        dayTransitionRoot.transform.SetAsLastSibling();
        dayTransitionText.text = $"DAY {CurrentDay}";
        dayTransitionText.fontSize = dayTransitionFontSize;
        bool startsCovered = startDayTransitionCovered;
        startDayTransitionCovered = false;
        screenFader.SetAlpha(startsCovered ? 1f : 0f);
        dayTextFader.SetAlpha(0f);

        float textSequenceDuration =
            dayTextFadeInDuration +
            dayTextDotInterval * 3f +
            dayTextHoldDuration +
            dayTextFadeOutDuration;
        float fadeInDuration = startsCovered ? 0f : screenFadeDuration;
        Coroutine screenTransition = screenFader.PlayFadeInOut(
            fadeInDuration,
            textSequenceDuration,
            screenFadeDuration);

        if (fadeInDuration > 0f)
            yield return new WaitForSecondsRealtime(fadeInDuration);

        ApplyDayBackground();

        Coroutine textFadeIn = dayTextFader.FadeIn(
            dayTextFadeInDuration);

        if (textFadeIn != null)
            yield return textFadeIn;

        for (int dotCount = 1; dotCount <= 3; dotCount++)
        {
            if (dayTextDotInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    dayTextDotInterval);
            }

            dayTransitionText.text =
                $"DAY {CurrentDay}{new string('.', dotCount)}";
        }

        if (dayTextHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(dayTextHoldDuration);

        Coroutine textFadeOut = dayTextFader.FadeOut(
            dayTextFadeOutDuration);

        if (textFadeOut != null)
            yield return textFadeOut;

        if (screenTransition != null)
            yield return screenTransition;

        if (CurrentDay == 1)
            firstDayTransitionCompleted = true;

        HideDayTransitionImmediately();
    }

    private void PrepareDayTransitionCovered()
    {
        if (!EnsureDayTransitionUI())
            return;

        dayTransitionRoot.SetActive(true);
        dayTransitionRoot.transform.SetAsLastSibling();
        dayTransitionText.text = string.Empty;
        screenFader.SetAlpha(1f);
        dayTextFader.SetAlpha(0f);
    }

    private void ApplyDayBackground()
    {
        if (worldBackgroundRenderer == null)
            return;

        Sprite selectedBackground =
            CurrentDay >= 6 && CurrentDay != 10
            ? daySixPlusBackground
            : dayOneToFiveBackground;

        if (selectedBackground != null)
            worldBackgroundRenderer.sprite = selectedBackground;
    }

    private bool EnsureDayTransitionUI()
    {
        if (dayTransitionRoot != null)
            return true;

        if (timerText == null || timerText.transform.parent == null)
        {
            Debug.LogWarning(
                "[MiniGameManager] Day transition UI requires the " +
                "common UI canvas.");
            return false;
        }

        Transform canvasTransform = timerText.transform.parent;
        int uiLayer = timerText.gameObject.layer;

        dayTransitionRoot = new GameObject(
            "DayTransition",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster));
        dayTransitionRoot.layer = uiLayer;
        RectTransform rootRect =
            dayTransitionRoot.GetComponent<RectTransform>();
        rootRect.SetParent(canvasTransform, false);
        StretchToParent(rootRect);

        Canvas transitionCanvas = dayTransitionRoot.GetComponent<Canvas>();
        transitionCanvas.overrideSorting = true;
        transitionCanvas.sortingOrder = dayTransitionSortingOrder;

        GameObject screenObject = new GameObject(
            "ScreenFade",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(CanvasGroupFader));
        screenObject.layer = uiLayer;
        RectTransform screenRect =
            screenObject.GetComponent<RectTransform>();
        screenRect.SetParent(rootRect, false);
        StretchToParent(screenRect);

        Image screenImage = screenObject.GetComponent<Image>();
        screenImage.color = Color.black;
        screenImage.raycastTarget = true;
        screenFader = screenObject.GetComponent<CanvasGroupFader>();
        screenFader.Configure(true, true, true);

        GameObject textObject = new GameObject(
            "DayText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(CanvasGroup),
            typeof(CanvasGroupFader));
        textObject.layer = uiLayer;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(rootRect, false);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(900f, 240f);

        dayTransitionText = textObject.GetComponent<TMP_Text>();
        dayTransitionText.font = timerText.font;
        dayTransitionText.fontSize = dayTransitionFontSize;
        dayTransitionText.alignment = TextAlignmentOptions.Center;
        dayTransitionText.color = Color.white;
        dayTransitionText.raycastTarget = false;
        dayTextFader = textObject.GetComponent<CanvasGroupFader>();
        dayTextFader.Configure(true, false, true);

        dayTransitionRoot.SetActive(false);
        return true;
    }

    private static void StretchToParent(RectTransform target)
    {
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }

    private void HideDayTransitionImmediately()
    {
        screenFader?.SetAlpha(0f);
        dayTextFader?.SetAlpha(0f);

        if (dayTransitionRoot != null)
            dayTransitionRoot.SetActive(false);
    }

    private bool SpawnMiniGame(
        MiniGameData data,
        int difficultyDay,
        float effectiveTimeLimit)
    {
        MiniGameBase selectedPrefab = data.SelectPrefabForDay(
            difficultyDay,
            out MiniGameAssetVariant selectedVariant);

        if (selectedPrefab == null)
        {
#if UNITY_EDITOR
            Debug.LogError(
                $"{data.name} has no prefab for the selected " +
                $"{selectedVariant} asset variant.");
#endif
            return false;
        }

        currentMiniGame = Instantiate(selectedPrefab, miniGameRoot);
        currentMiniGameIsBonus = data.isBonus;
        currentMiniGame.SetAssetVariant(selectedVariant);
#if UNITY_EDITOR
        Debug.Log(
            $"[MiniGameManager] {data.name} asset variant: " +
            $"{selectedVariant} (day {CurrentDay}).");
#endif
        currentMiniGame.OnFinished += HandleMiniGameFinished;
        currentMiniGame.OnTimerResetRequested += HandleTimerResetRequested;
        currentMiniGame.Init(
            data,
            CurrentDay,
            difficultyDay,
            effectiveTimeLimit);
        currentMiniGame.Play();

        if (isPaused)
            currentMiniGame.enabled = false;

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
            if (result.NextDayStudyMultiplier > 1f)
            {
                DaySystem.Instance.ScheduleNextDayStudyMultiplier(
                    result.NextDayStudyMultiplier);
            }

            int studyReward = result.StudyRewardOverride ??
                playerStatus.Settings.StudyAmountPerMiniGame;

            if (!result.IsBonus)
                studyReward = DaySystem.Instance.ApplyStudyMultiplier(studyReward);

            playerStatus.AddStudyAmount(studyReward);
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
        HideInstructionImmediately();
        HideDayTransitionImmediately();
        DestroyCurrentMiniGame();
        lastMiniGameResult = null;
    }

    private void DestroyCurrentMiniGame()
    {
        if (currentMiniGame == null)
        {
            currentMiniGameIsBonus = false;
            return;
        }

        currentMiniGame.OnFinished -= HandleMiniGameFinished;
        currentMiniGame.OnTimerResetRequested -= HandleTimerResetRequested;
        currentMiniGame.Stop();
        Destroy(currentMiniGame.gameObject);
        currentMiniGame = null;
        currentMiniGameIsBonus = false;
        timerResetRequested = false;
    }
}

public static class KoreanFontBootstrap
{
    private const string FontResourcePath = "Fonts/Galmuri9";

    public static TMP_FontAsset FontAsset { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (FontAsset != null)
            return;

        Font sourceFont = Resources.Load<Font>(FontResourcePath);

        if (sourceFont == null)
        {
            Debug.LogError(
                $"[KoreanFontBootstrap] Font resource '{FontResourcePath}' " +
                "could not be loaded.");
            return;
        }

        FontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            4,
            GlyphRenderMode.SDFAA_HINTED,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);

        if (FontAsset == null)
        {
            Debug.LogError(
                "[KoreanFontBootstrap] Failed to create the Galmuri9 " +
                "TMP font asset.");
            return;
        }

        FontAsset.name = "Galmuri9 Dynamic SDF";
        List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;

        if (fallbacks != null && !fallbacks.Contains(FontAsset))
            fallbacks.Add(FontAsset);
    }
}
