using System;
using UnityEngine;

[Serializable]
public class BonusMiniGameDayConfiguration
{
    [Min(1)]
    [SerializeField] private int startDay = 1;
    [SerializeField] private MiniGameData firstBonus = CreateBonusData();
    [SerializeField] private MiniGameData secondBonus = CreateBonusData();

    public int StartDay => Mathf.Max(1, startDay);
    public MiniGameData FirstBonus => firstBonus;
    public MiniGameData SecondBonus => secondBonus;

    public void EnsureBonusFlags()
    {
        if (firstBonus != null)
            firstBonus.isBonus = true;

        if (secondBonus != null)
            secondBonus.isBonus = true;
    }

    private static MiniGameData CreateBonusData()
    {
        return new MiniGameData
        {
            isBonus = true
        };
    }
}
