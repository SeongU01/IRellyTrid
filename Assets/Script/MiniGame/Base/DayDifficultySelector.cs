using System.Collections.Generic;

public static class DayDifficultySelector
{
    public static T GetForDay<T>(
        IReadOnlyList<T> difficulties,
        int currentDay)
        where T : DayDifficultyData
    {
        if (difficulties == null || difficulties.Count == 0)
        {
            return null;
        }

        T selected = null;
        T earliest = null;

        for (int i = 0; i < difficulties.Count; i++)
        {
            T difficulty = difficulties[i];

            if (difficulty == null)
            {
                continue;
            }

            if (earliest == null ||
                difficulty.StartDay < earliest.StartDay)
            {
                earliest = difficulty;
            }

            if (difficulty.StartDay <= currentDay &&
                (selected == null ||
                 difficulty.StartDay > selected.StartDay))
            {
                selected = difficulty;
            }
        }

        return selected ?? earliest;
    }
}
