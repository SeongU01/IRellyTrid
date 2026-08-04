using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FatigueRecoveryController : MonoBehaviour
{
    private DaySystem daySystem;
    private PlayerStatus playerStatus;
    private bool recoveryAppliedForCurrentDay;

    public event Action<int, float, float> OnRecoveryApplied;

    private void Awake()
    {
        daySystem = DaySystem.Instance;
        playerStatus = PlayerStatus.Instance;

        if (daySystem != null && playerStatus != null)
            return;

        Debug.LogError(
            "[FatigueRecoveryController] Required status systems are " +
            "missing.");
        enabled = false;
    }

    private void OnEnable()
    {
        daySystem = DaySystem.Instance;
        playerStatus = PlayerStatus.Instance;

        if (daySystem == null)
            return;

        daySystem.OnDayStarted += HandleDayStarted;
        daySystem.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (daySystem == null)
            return;

        daySystem.OnDayStarted -= HandleDayStarted;
        daySystem.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandleDayStarted(int day)
    {
        recoveryAppliedForCurrentDay = false;
    }

    private void HandlePhaseChanged(DayPhase previous, DayPhase current)
    {
        if (current != DayPhase.Recovery ||
            recoveryAppliedForCurrentDay)
        {
            return;
        }

        ApplyRecovery();
    }

    private void ApplyRecovery()
    {
        if (daySystem == null || playerStatus == null ||
            playerStatus.IsGameOver || playerStatus.Settings == null)
        {
            return;
        }

        recoveryAppliedForCurrentDay = true;

        int performedBonusGames = Mathf.Clamp(
            daySystem.PerformedBonusMiniGames,
            0,
            2);
        float requestedRecovery = GetRecoveryAmount(
            performedBonusGames,
            playerStatus.Settings);
        float fatigueBeforeRecovery = playerStatus.Fatigue;

        playerStatus.RecoverFatigue(requestedRecovery);

        float actualRecovery =
            fatigueBeforeRecovery - playerStatus.Fatigue;
        Debug.Log(
            $"[FatigueRecoveryController] Day {daySystem.CurrentDay}: " +
            $"performed bonus games {performedBonusGames}/2, " +
            $"requested recovery {requestedRecovery:0.##}, " +
            $"fatigue {fatigueBeforeRecovery:0.##} -> " +
            $"{playerStatus.Fatigue:0.##} " +
            $"(recovered {actualRecovery:0.##}).");

        OnRecoveryApplied?.Invoke(
            performedBonusGames,
            requestedRecovery,
            actualRecovery);
    }

    private static float GetRecoveryAmount(
        int performedBonusGames,
        GameBalanceSettings settings)
    {
        switch (performedBonusGames)
        {
            case 0:
                return settings.SleepRecoveryAmount;
            case 1:
                return settings.OneBonusRecoveryAmount;
            default:
                return settings.TwoBonusRecoveryAmount;
        }
    }
}
