using UnityEngine;

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
    }
}
