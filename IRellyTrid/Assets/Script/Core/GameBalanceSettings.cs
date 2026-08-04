using System;
using UnityEngine;

[Serializable]
public sealed class GameDaySettings
{
    [SerializeField, Min(1)] private int startDay = 1;
    [SerializeField] private bool randomizeMiniGameOrder;
    [SerializeField, Min(0.1f)] private float miniGameTimeLimit = 30f;
    [Tooltip("0이면 실제 일차의 미니게임 난이도를 사용합니다.")]
    [SerializeField, Min(0)] private int miniGameDifficultyDay;
    [Tooltip("비어 있는 슬롯은 MiniGameManager의 기본 미니게임을 사용합니다.")]
    [SerializeField] private MiniGameData[] miniGameOverrides;

    [Header("Eye")]
    [SerializeField, Min(1)] private int eyeSpaceTapsPerStep = 3;

    [Header("Popup")]
    [SerializeField, Range(0f, 1f)] private float popupChancePerSecond;
    [SerializeField, Min(0)] private int minimumPopupCount;
    [SerializeField, Min(0)] private int maximumPopupCount;

    public int StartDay => Mathf.Max(1, startDay);
    public bool RandomizeMiniGameOrder => randomizeMiniGameOrder;
    public float MiniGameTimeLimit => Mathf.Max(0.1f, miniGameTimeLimit);
    public int MiniGameDifficultyDay => Mathf.Max(0, miniGameDifficultyDay);
    public int EyeSpaceTapsPerStep => Mathf.Max(1, eyeSpaceTapsPerStep);
    public float PopupChancePerSecond =>
        Mathf.Clamp01(popupChancePerSecond);
    public int MinimumPopupCount => Mathf.Max(0, minimumPopupCount);
    public int MaximumPopupCount => Mathf.Max(
        MinimumPopupCount,
        maximumPopupCount);
    public bool PopupEnabled =>
        PopupChancePerSecond > 0f && MaximumPopupCount > 0;

    public MiniGameData GetMiniGameOverride(int index)
    {
        if (miniGameOverrides == null ||
            index < 0 ||
            index >= miniGameOverrides.Length)
        {
            return null;
        }

        MiniGameData data = miniGameOverrides[index];
        return data != null && data.prefab != null ? data : null;
    }

    public void Validate()
    {
        startDay = Mathf.Max(1, startDay);
        miniGameTimeLimit = Mathf.Max(0.1f, miniGameTimeLimit);
        miniGameDifficultyDay = Mathf.Max(0, miniGameDifficultyDay);
        eyeSpaceTapsPerStep = Mathf.Max(1, eyeSpaceTapsPerStep);
        popupChancePerSecond = Mathf.Clamp01(popupChancePerSecond);
        minimumPopupCount = Mathf.Max(0, minimumPopupCount);
        maximumPopupCount = Mathf.Max(minimumPopupCount, maximumPopupCount);
    }
}

[CreateAssetMenu(
    fileName = "GameBalanceSettings",
    menuName = "Game/Balance Settings")]
public class GameBalanceSettings : ScriptableObject
{
    [Header("Player Status")]
    [Min(1)]
    [SerializeField] private int maxHealth = 5;
    [Min(1f)]
    [SerializeField] private float maxFatigue = 100f;

    [Header("Eye")]
    [Min(0.1f)]
    [SerializeField] private float eyeCloseDuration = 20f;
    [Min(0f)]
    [SerializeField] private float eyeRecoveryPerSpace = 2f;
    [Min(0.1f)]
    [SerializeField] private float closedEyeDamageInterval = 5f;

    [Header("Mini Game")]
    [Min(0)]
    [SerializeField] private int studyAmountPerMiniGame = 5;
    [Min(0f)]
    [SerializeField] private float fatiguePerSecond = 0.5f;
    [Min(1)]
    [SerializeField] private int normalMiniGamesPerDay = 5;
    [Min(0)]
    [SerializeField] private int failureHealthDamage = 1;
    [Min(0)]
    [SerializeField] private int timeoutHealthDamage = 1;
    [Min(0f)]
    [SerializeField] private float miniGameReadyDuration = 1f;
    [Min(0f)]
    [SerializeField] private float miniGameResultDuration = 1f;

    [Header("Recovery")]
    [Min(0f)]
    [SerializeField] private float sleepRecoveryAmount = 50f;
    [Min(0f)]
    [SerializeField] private float oneBonusRecoveryAmount = 20f;
    [Min(0f)]
    [SerializeField] private float twoBonusRecoveryAmount;

    [Header("Day")]
    [Min(1)]
    [SerializeField] private int finalDay = 10;
    [SerializeField] private GameDaySettings[] daySettings;

    public int MaxHealth => maxHealth;
    public float MaxFatigue => maxFatigue;
    public float EyeCloseDuration => eyeCloseDuration;
    public float EyeRecoveryPerSpace => eyeRecoveryPerSpace;
    public float ClosedEyeDamageInterval => closedEyeDamageInterval;
    public int StudyAmountPerMiniGame => studyAmountPerMiniGame;
    public float FatiguePerSecond => fatiguePerSecond;
    public int NormalMiniGamesPerDay => normalMiniGamesPerDay;
    public int FailureHealthDamage => failureHealthDamage;
    public int TimeoutHealthDamage => timeoutHealthDamage;
    public float MiniGameReadyDuration => miniGameReadyDuration;
    public float MiniGameResultDuration => miniGameResultDuration;
    public float SleepRecoveryAmount => sleepRecoveryAmount;
    public float OneBonusRecoveryAmount => oneBonusRecoveryAmount;
    public float TwoBonusRecoveryAmount => twoBonusRecoveryAmount;
    public int FinalDay => finalDay;

    public GameDaySettings GetDaySettings(int day)
    {
        if (daySettings == null || daySettings.Length == 0)
            return null;

        int targetDay = Mathf.Max(1, day);
        GameDaySettings selected = null;
        GameDaySettings earliest = null;

        for (int i = 0; i < daySettings.Length; i++)
        {
            GameDaySettings settings = daySettings[i];

            if (settings == null)
                continue;

            if (earliest == null || settings.StartDay < earliest.StartDay)
                earliest = settings;

            if (settings.StartDay <= targetDay &&
                (selected == null || settings.StartDay > selected.StartDay))
            {
                selected = settings;
            }
        }

        return selected ?? earliest;
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        maxFatigue = Mathf.Max(1f, maxFatigue);
        eyeCloseDuration = Mathf.Max(0.1f, eyeCloseDuration);
        eyeRecoveryPerSpace = Mathf.Max(0f, eyeRecoveryPerSpace);
        closedEyeDamageInterval = Mathf.Max(0.1f, closedEyeDamageInterval);
        studyAmountPerMiniGame = Mathf.Max(0, studyAmountPerMiniGame);
        fatiguePerSecond = Mathf.Max(0f, fatiguePerSecond);
        normalMiniGamesPerDay = Mathf.Max(1, normalMiniGamesPerDay);
        failureHealthDamage = Mathf.Max(0, failureHealthDamage);
        timeoutHealthDamage = Mathf.Max(0, timeoutHealthDamage);
        miniGameReadyDuration = Mathf.Max(0f, miniGameReadyDuration);
        miniGameResultDuration = Mathf.Max(0f, miniGameResultDuration);
        sleepRecoveryAmount = Mathf.Max(0f, sleepRecoveryAmount);
        oneBonusRecoveryAmount = Mathf.Max(0f, oneBonusRecoveryAmount);
        twoBonusRecoveryAmount = Mathf.Max(0f, twoBonusRecoveryAmount);
        finalDay = Mathf.Max(1, finalDay);

        if (daySettings == null)
            return;

        for (int i = 0; i < daySettings.Length; i++)
            daySettings[i]?.Validate();
    }
}
