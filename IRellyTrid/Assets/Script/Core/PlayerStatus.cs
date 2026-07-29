using System;
using UnityEngine;

public sealed class PlayerStatus : MonoBehaviour
{
    private const string SettingsResourceName = "GameBalanceSettings";

    private static PlayerStatus instance;

    [SerializeField] private GameBalanceSettings settings;

    private int health;
    private float fatigue;
    private int studyAmount;
    private bool isGameOver;

    public static PlayerStatus Instance
    {
        get
        {
            if (instance == null && Application.isPlaying)
                CreateRuntimeInstance();

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        CreateRuntimeInstance();
    }

    private static void CreateRuntimeInstance()
    {
        if (instance != null)
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
            instance = null;
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

        studyAmount += amount;
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
        health = Mathf.Clamp(value, 0, settings.MaxHealth);
        HealthChanged?.Invoke(health, settings.MaxHealth);

        if (health <= 0)
            TriggerGameOver(GameOverReason.HealthDepleted);
    }

    private void SetFatigue(float value)
    {
        fatigue = Mathf.Clamp(value, 0f, settings.MaxFatigue);
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
