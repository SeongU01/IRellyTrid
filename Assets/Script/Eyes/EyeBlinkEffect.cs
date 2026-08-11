using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EyeBlinkEffect : MonoBehaviour
{
    [SerializeField] private MiniGameManager miniGameManager;

    [Header("Frame Animation")]
    [SerializeField] private Image animatedEyesImage;
    [SerializeField] private Sprite[] earlyDayEyeFrames;
    [SerializeField] private Sprite[] lateDayEyeFrames;
    [SerializeField, Min(1)] private int lateDayStartDay = 6;

    private float maxCloseTime = 20f;
    private float currentCloseTime;
    private float fullyClosedTimer;
    private float damageInterval = 5f;
    private int configuredDay = -1;
    private int spaceTapCount;
    private int spaceTapsPerStep = 1;

    private void Start()
    {
        ApplyBalanceSettings();
        RefreshDayConfiguration(true);
        UpdateEyeFrame();
    }

    private void Update()
    {
        if (!IsMiniGamePlaying())
            return;

        RefreshDayConfiguration(false);
        currentCloseTime = Mathf.Clamp(
            currentCloseTime + Time.deltaTime,
            0f,
            maxCloseTime);

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null &&
            keyboard.spaceKey.wasPressedThisFrame)
        {
            HandleSpaceRecovery();
        }

        UpdateEyeFrame();
        CheckDamageCondition();
    }

    private void ApplyBalanceSettings()
    {
        PlayerStatus playerStatus = PlayerStatus.Instance;

        if (playerStatus == null || playerStatus.Settings == null)
            return;

        maxCloseTime = Mathf.Max(
            0.1f,
            playerStatus.Settings.EyeCloseDuration);
        damageInterval = Mathf.Max(
            0.1f,
            playerStatus.Settings.ClosedEyeDamageInterval);
    }

    private void RefreshDayConfiguration(bool force)
    {
        DaySystem daySystem = DaySystem.Instance;
        int currentDay = daySystem != null
            ? Mathf.Max(1, daySystem.CurrentDay)
            : 1;

        if (!force && configuredDay == currentDay)
            return;

        configuredDay = currentDay;
        spaceTapCount = 0;
        spaceTapsPerStep = 1;

        PlayerStatus playerStatus = PlayerStatus.Instance;

        if (playerStatus != null && playerStatus.Settings != null)
        {
            GameDaySettings daySettings =
                playerStatus.Settings.GetDaySettings(currentDay);

            if (daySettings != null)
            {
                spaceTapsPerStep =
                    Mathf.Max(1, daySettings.EyeSpaceTapsPerStep);
            }
        }

        UpdateEyeFrame();
    }

    private void HandleSpaceRecovery()
    {
        spaceTapCount++;

        if (spaceTapCount < spaceTapsPerStep)
            return;

        spaceTapCount = 0;
        Sprite[] frames = GetActiveEyeFrames();
        int frameStepCount = Mathf.Max(
            1,
            frames != null ? frames.Length - 1 : 1);
        float recoveryPerStep = maxCloseTime / frameStepCount;
        currentCloseTime = Mathf.Max(
            0f,
            currentCloseTime - recoveryPerStep);
        fullyClosedTimer = 0f;
    }

    private void UpdateEyeFrame()
    {
        if (animatedEyesImage == null)
            return;

        Sprite[] frames = GetActiveEyeFrames();

        if (frames == null || frames.Length == 0)
            return;

        float ratio = maxCloseTime > 0f
            ? Mathf.Clamp01(currentCloseTime / maxCloseTime)
            : 0f;
        int frameIndex = Mathf.Clamp(
            Mathf.RoundToInt(ratio * (frames.Length - 1)),
            0,
            frames.Length - 1);
        animatedEyesImage.sprite = frames[frameIndex];
        animatedEyesImage.enabled = true;
    }

    private Sprite[] GetActiveEyeFrames()
    {
        bool useLateDayFrames = configuredDay >= lateDayStartDay;

        if (!useLateDayFrames &&
            earlyDayEyeFrames != null &&
            earlyDayEyeFrames.Length > 0)
        {
            return earlyDayEyeFrames;
        }

        if (lateDayEyeFrames != null && lateDayEyeFrames.Length > 0)
            return lateDayEyeFrames;

        return earlyDayEyeFrames;
    }

    private void CheckDamageCondition()
    {
        if (currentCloseTime < maxCloseTime)
        {
            fullyClosedTimer = 0f;
            return;
        }

        fullyClosedTimer += Time.deltaTime;

        if (fullyClosedTimer < damageInterval)
            return;

        PlayerStatus playerStatus = PlayerStatus.Instance;

        if (playerStatus != null)
            playerStatus.TakeDamage(1);

        fullyClosedTimer = 0f;
        currentCloseTime = 0f;
        spaceTapCount = 0;
        UpdateEyeFrame();
    }

    private bool IsMiniGamePlaying()
    {
        if (miniGameManager == null)
            miniGameManager = FindAnyObjectByType<MiniGameManager>();

        return miniGameManager != null &&
            miniGameManager.IsMiniGamePlaying;
    }
}
