using System;
using UnityEngine;

public sealed class PlayerStatus : MonoBehaviour
{
    private const string SettingsResourceName = "GameBalanceSettings";

    private static PlayerStatus instance;
    private static bool isShuttingDown;

    [SerializeField] private GameBalanceSettings settings;

    private int health;
    private float fatigue;
    private int studyAmount;
    private bool isGameOver;

    public static PlayerStatus Instance
    {
        get
        {
            if (instance == null &&
                Application.isPlaying &&
                !isShuttingDown)
            {
                CreateRuntimeInstance();
            }

            return instance;
        }
    }

    public GameBalanceSettings Settings => settings;
    public int Health => health;
    public int MaxHealth => settings != null ? settings.MaxHealth : 0;
    public float Fatigue => fatigue;
    public float MaxFatigue => settings != null ? settings.MaxFatigue : 0f;
    public int StudyAmount => studyAmount;
    public bool IsGameOver => isGameOver;

    public event Action<int, int> HealthChanged;
    public event Action<float, float> FatigueChanged;
    public event Action<int> StudyAmountChanged;
    public event Action<GameOverReason> GameOverTriggered;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isShuttingDown = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        CreateRuntimeInstance();
    }

    private static void CreateRuntimeInstance()
    {
        if (instance != null || isShuttingDown)
            return;

        GameObject statusObject = new GameObject(nameof(PlayerStatus));
        statusObject.AddComponent<PlayerStatus>();
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

        LoadSettings();
        ResetStatus();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            isShuttingDown = true;
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || isGameOver)
            return;

        SetHealth(health - amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || isGameOver)
            return;

        SetHealth(health + amount);
    }

    public void AddFatigue(float amount)
    {
        if (amount <= 0f || isGameOver)
            return;

        SetFatigue(fatigue + amount);
    }

    public void RecoverFatigue(float amount)
    {
        if (amount <= 0f || isGameOver)
            return;

        SetFatigue(fatigue - amount);
    }

    public void AddStudyAmount(int amount)
    {
        if (amount <= 0 || isGameOver)
            return;

        int previousStudyAmount = studyAmount;
        studyAmount += amount;
        Debug.Log(
            $"[PlayerStatus] Study Amount: {previousStudyAmount} -> " +
            $"{studyAmount} (+{studyAmount - previousStudyAmount})");
        StudyAmountChanged?.Invoke(studyAmount);
    }

    public void ResetStatus()
    {
        if (settings == null)
            LoadSettings();

        health = settings.MaxHealth;
        fatigue = 0f;
        studyAmount = 0;
        isGameOver = false;

        Debug.Log(
            $"[PlayerStatus] Reset - Health: {health}/{settings.MaxHealth}, " +
            $"Fatigue: {fatigue:0.##}/{settings.MaxFatigue:0.##}, " +
            $"Study Amount: {studyAmount}");
        HealthChanged?.Invoke(health, settings.MaxHealth);
        FatigueChanged?.Invoke(fatigue, settings.MaxFatigue);
        StudyAmountChanged?.Invoke(studyAmount);
    }

    private void LoadSettings()
    {
        if (settings != null)
            return;

        settings = Resources.Load<GameBalanceSettings>(SettingsResourceName);

        if (settings != null)
            return;

        settings = ScriptableObject.CreateInstance<GameBalanceSettings>();
        Debug.LogWarning(
            $"{SettingsResourceName} was not found in a Resources folder. " +
            "Runtime default balance settings will be used.");
    }

    private void SetHealth(int value)
    {
        int previousHealth = health;
        health = Mathf.Clamp(value, 0, settings.MaxHealth);

        if (health == previousHealth)
            return;

        int change = health - previousHealth;
        Debug.Log(
            $"[PlayerStatus] Health: {previousHealth} -> {health} " +
            $"({change:+#;-#;0})");
        HealthChanged?.Invoke(health, settings.MaxHealth);

        if (health <= 0)
            TriggerGameOver(GameOverReason.HealthDepleted);
    }

    private void SetFatigue(float value)
    {
        float previousFatigue = fatigue;
        fatigue = Mathf.Clamp(value, 0f, settings.MaxFatigue);

        if (Mathf.Approximately(fatigue, previousFatigue))
            return;

        bool decreased = fatigue < previousFatigue;
        bool crossedWholeNumber =
            Mathf.FloorToInt(fatigue) != Mathf.FloorToInt(previousFatigue);

        if (decreased || crossedWholeNumber)
        {
            float change = fatigue - previousFatigue;
            Debug.Log(
                $"[PlayerStatus] Fatigue: {previousFatigue:0.##} -> " +
                $"{fatigue:0.##} ({change:+0.##;-0.##;0})");
        }

        FatigueChanged?.Invoke(fatigue, settings.MaxFatigue);

        if (fatigue >= settings.MaxFatigue)
            TriggerGameOver(GameOverReason.FatigueMaxed);
    }

    private void TriggerGameOver(GameOverReason reason)
    {
        if (isGameOver)
            return;

        isGameOver = true;
        GameOverTriggered?.Invoke(reason);
    }
}
