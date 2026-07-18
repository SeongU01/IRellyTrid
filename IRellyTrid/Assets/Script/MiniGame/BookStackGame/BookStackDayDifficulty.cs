using UnityEngine;

[System.Serializable]
public class BookStackDayDifficulty : DayDifficultyData
{
    [Header("Pile")]
    public Vector2 pileArea = new Vector2(4f, 3f);

    [Range(0f, 180f)]
    public float maxSpawnRotation = 60f;

    [Header("Stages")]
    public BookStageData[] stages =
    {
        new BookStageData { bookCount = 4, similarBookCount = 0 },
        new BookStageData { bookCount = 5, similarBookCount = 2 },
        new BookStageData { bookCount = 6, similarBookCount = 3 },
        new BookStageData { bookCount = 7, similarBookCount = 4 },
        new BookStageData { bookCount = 8, similarBookCount = 5 }
    };

    [Header("Book Images")]
    public BookData[] targetBooks;
    public BookData[] similarBooks;
    public BookData[] normalBooks;
}
