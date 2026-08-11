using UnityEngine;

public enum MiniGameAssetVariant
{
    Base,
    First,
    Second
}

[System.Serializable]
public class MiniGameData
{
    public string name;
    public string commandText;
    public float timeLimit = 30f;
    public bool isBonus;
    public MiniGameBase prefab;

    [Header("Asset Variants")]
    public bool useAssetVariants;
    public bool assetVariantPrefabsContainMixedAssets;
    public MiniGameBase firstAssetVariantPrefab;
    public MiniGameBase secondAssetVariantPrefab;
    [Min(1)] public int firstAssetVariantUnlockDay = 1;
    [Min(1)] public int secondAssetVariantUnlockDay = 6;

    public MiniGameBase SelectPrefabForDay(
        int currentDay,
        out MiniGameAssetVariant selectedVariant)
    {
        selectedVariant = MiniGameAssetVariant.Base;

        if (!useAssetVariants)
            return prefab;

        int day = Mathf.Max(1, currentDay);
        int firstUnlockDay = Mathf.Max(
            1,
            firstAssetVariantUnlockDay);
        int secondUnlockDay = Mathf.Max(
            firstUnlockDay,
            secondAssetVariantUnlockDay);

        if (assetVariantPrefabsContainMixedAssets)
        {
            if (day >= secondUnlockDay)
            {
                if (secondAssetVariantPrefab == null)
                {
                    LogMissingVariantWarning(day, true);
                    return prefab;
                }

                selectedVariant = MiniGameAssetVariant.Second;
                return secondAssetVariantPrefab;
            }

            if (day < firstUnlockDay)
                return prefab;

            if (firstAssetVariantPrefab == null)
            {
                LogMissingVariantWarning(day, false);
                return prefab;
            }

            selectedVariant = MiniGameAssetVariant.First;
            return firstAssetVariantPrefab;
        }

        if (day >= secondUnlockDay)
        {
            if (firstAssetVariantPrefab == null ||
                secondAssetVariantPrefab == null)
            {
                LogMissingVariantWarning(day, true);
                return prefab;
            }

            int selection = Random.Range(0, 3);
            selectedVariant = (MiniGameAssetVariant)selection;

            return selection switch
            {
                1 => firstAssetVariantPrefab,
                2 => secondAssetVariantPrefab,
                _ => prefab
            };
        }

        if (day < firstUnlockDay)
            return prefab;

        if (firstAssetVariantPrefab == null)
        {
            LogMissingVariantWarning(day, false);
            return prefab;
        }

        if (Random.Range(0, 2) == 0)
            return prefab;

        selectedVariant = MiniGameAssetVariant.First;
        return firstAssetVariantPrefab;
    }

    private void LogMissingVariantWarning(int currentDay, bool secondStage)
    {
        string missingVariants = secondStage
            ? "first and/or second variant prefab"
            : "first variant prefab";
        Debug.LogWarning(
            $"[MiniGameData] {name} day {currentDay}: " +
            $"{missingVariants} is missing. The base prefab will be used.");
    }
}
