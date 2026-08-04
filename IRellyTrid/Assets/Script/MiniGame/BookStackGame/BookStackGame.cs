using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookStackGame : MiniGameBase
{
    [Header("UI")]
    [SerializeField] private Image targetImage;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text resultText;

    [Header("Book Spawn")]
    [SerializeField] private Transform bookRoot;
    [SerializeField] private Transform pileCenter;
    [SerializeField] private BookItem bookPrefab;

    [Header("Drop Zone")]
    [SerializeField] private BookDropZone dropZone;

    [Header("Day Difficulties")]
    [SerializeField]
    private BookStackDayDifficulty[] dayDifficulties;

    private readonly List<BookItem> activeBooks =
        new List<BookItem>();

    private readonly List<BookItem> placedBooks =
        new List<BookItem>();

    private readonly List<BookData> remainingTargetBooks =
        new List<BookData>();

    private int currentStageIndex;
    private int nextSortingOrder;
    private BookStackDayDifficulty currentDifficulty;

    private BookStageData[] Stages =>
        currentDifficulty != null
            ? currentDifficulty.stages
            : null;

    private BookData[] TargetBooks =>
        currentDifficulty != null
            ? currentDifficulty.targetBooks
            : null;

    private BookData[] SimilarBooks =>
        currentDifficulty != null
            ? currentDifficulty.similarBooks
            : null;

    private BookData[] NormalBooks =>
        currentDifficulty != null
            ? currentDifficulty.normalBooks
            : null;

    protected override void OnStart()
    {
        currentDifficulty =
            DayDifficultySelector.GetForDay(
                dayDifficulties,
                DifficultyDay);

        if (currentDifficulty == null)
        {
#if UNITY_EDITOR
            Debug.LogError("책 게임 일차 설정이 없습니다.");
#endif
            Fail();
            return;
        }

        currentStageIndex = 0;
        nextSortingOrder = 2000;

        ClearAllBooks();
        CreateTargetBookPool();

        if (resultText != null)
        {
            resultText.text = "";
        }

        if (targetImage != null)
        {
            targetImage.sprite = null;
            targetImage.enabled = false;
        }

        if (Stages != null &&
            remainingTargetBooks.Count < Stages.Length)
        {
#if UNITY_EDITOR
            Debug.LogError(
                $"목표 책이 부족합니다. " +
                $"필요: {Stages.Length}, " +
                $"등록: {remainingTargetBooks.Count}");
#endif
            Fail();
            return;
        }

        CreateStage();
    }

   private void CreateTargetBookPool()
{
    remainingTargetBooks.Clear();

    if (TargetBooks == null)
    {
        return;
    }

    HashSet<Sprite> addedSprites =
        new HashSet<Sprite>();

    for (int i = 0; i < TargetBooks.Length; i++)
    {
        BookData targetBook = TargetBooks[i];

        if (targetBook == null ||
            targetBook.sprite == null)
        {
            continue;
        }

        // 같은 스프라이트가 여러 번 등록되어도 한 번만 추가한다.
        if (!addedSprites.Add(targetBook.sprite))
        {
            continue;
        }

        remainingTargetBooks.Add(targetBook);
    }

    Shuffle(remainingTargetBooks);
}

private BookData GetNextTargetBook()
{
    if (remainingTargetBooks.Count == 0)
    {
        return null;
    }

    int lastIndex = remainingTargetBooks.Count - 1;
    BookData targetBook =
        remainingTargetBooks[lastIndex];

    remainingTargetBooks.RemoveAt(lastIndex);
    return targetBook;
}
    private void CreateStage()
    {
        ClearActiveBooks();

        if (!ValidateSettings())
        {
            Fail();
            return;
        }

        BookStageData stage = Stages[currentStageIndex];

        if (stage == null)
        {
#if UNITY_EDITOR
            Debug.LogError("현재 스테이지 데이터가 없습니다.");
#endif
            Fail();
            return;
        }

        BookData targetBook = GetNextTargetBook();

        if (targetBook == null)
        {
#if UNITY_EDITOR
            Debug.LogError("유효한 목표 책 데이터가 없습니다.");
#endif
            Fail();
            return;
        }

        int bookCount = Mathf.Max(1, stage.bookCount);

        int similarBookCount = Mathf.Clamp(
            stage.similarBookCount,
            0,
            bookCount - 1);

        int normalBookCount =
            bookCount - similarBookCount - 1;

        BookData matchingSimilarBook =
            GetMatchingSimilarBook(targetBook);

        if (matchingSimilarBook == null)
        {
#if UNITY_EDITOR
            Debug.LogError(
                $"목표 책과 짝이 맞는 비슷한 책이 없습니다: " +
                $"{targetBook.bookName}");
#endif
            Fail();
            return;
        }

        List<(BookData data, bool isTarget)> stageBooks =
            new List<(BookData, bool)>();

        stageBooks.Add((targetBook, true));

        for (int i = 0; i < similarBookCount; i++)
        {
            stageBooks.Add((matchingSimilarBook, false));
        }

        List<BookData> selectedNormalBooks =
            GetUniqueRandomBooks(
                NormalBooks,
                normalBookCount,
                matchingSimilarBook.sprite);

        if (selectedNormalBooks.Count < normalBookCount)
        {
#if UNITY_EDITOR
            Debug.LogError(
                $"서로 다른 일반책이 부족합니다. " +
                $"필요: {normalBookCount}, " +
                $"사용 가능: {selectedNormalBooks.Count}");
#endif
            Fail();
            return;
        }

        for (int i = 0; i < selectedNormalBooks.Count; i++)
        {
            stageBooks.Add((selectedNormalBooks[i], false));
        }

        Shuffle(stageBooks);
        SpawnScatteredBooks(stageBooks);

        if (targetImage != null)
        {
            targetImage.sprite = targetBook.sprite;
            targetImage.color = Color.white;
            targetImage.preserveAspect = true;
            targetImage.enabled = true;
        }

        if (progressText != null)
        {
            progressText.text =
                $"{currentStageIndex + 1} / {Stages.Length}";
        }

#if UNITY_EDITOR
        Debug.Log(
            $"Stage {currentStageIndex + 1} 시작 " +
            $"/ 목표 책: {targetBook.bookName}");
#endif
    }

    public void BeginBookDrag(BookItem book)
    {
        if (!IsPlaying || book == null || book.IsLocked)
        {
            return;
        }

        nextSortingOrder++;

        float frontZ =
            -1f - nextSortingOrder * 0.0001f;

        book.BringToFront(
            nextSortingOrder,
            frontZ);

        if (resultText != null)
        {
            resultText.text = "";
        }
    }

    public void OnBookDropped(BookItem book)
    {
        if (!IsPlaying || book == null || book.IsLocked)
        {
            return;
        }

        if (!IsBookInsideDropZone(book))
        {
            return;
        }

        if (!book.IsTarget)
        {
            if (resultText != null)
            {
                resultText.text = "X";
            }

#if UNITY_EDITOR
            Debug.Log($"잘못된 책: {book.BookName}");
#endif
            Fail();
            return;
        }

        PlaceTargetBook(book);
        NextStage();
    }

    private bool IsBookInsideDropZone(BookItem book)
    {
        if (dropZone == null || book == null)
        {
            return false;
        }

        return dropZone.Contains(
            book.transform.position);
    }

    private void PlaceTargetBook(BookItem book)
    {
        activeBooks.Remove(book);

        int placedIndex = placedBooks.Count;
        placedBooks.Add(book);

        Vector3 localPosition =
            dropZone.GetPlacedLocalPosition(placedIndex);

        int sortingOrder = 3000 + placedIndex;

        book.LockAt(
            dropZone.PlacedBookRoot,
            localPosition,
            sortingOrder);

        if (resultText != null)
        {
            resultText.text = "O";
        }

#if UNITY_EDITOR
        Debug.Log($"목표 책 배치 완료: {book.BookName}");
#endif
    }

    private void SpawnScatteredBooks(
        List<(BookData data, bool isTarget)> stageBooks)
    {
        for (int i = 0; i < stageBooks.Count; i++)
        {
            float randomX = Random.Range(
                -currentDifficulty.pileArea.x * 0.5f,
                currentDifficulty.pileArea.x * 0.5f);

            float randomY = Random.Range(
                -currentDifficulty.pileArea.y * 0.5f,
                currentDifficulty.pileArea.y * 0.5f);

            float randomRotation = Random.Range(
                -currentDifficulty.maxSpawnRotation,
                currentDifficulty.maxSpawnRotation);

            Vector3 worldPosition =
                pileCenter.position +
                new Vector3(
                    randomX,
                    randomY,
                    -i * 0.01f);

            BookItem book = Instantiate(
                bookPrefab,
                worldPosition,
                Quaternion.Euler(
                    0f,
                    0f,
                    randomRotation),
                bookRoot);

            book.Init(
                this,
                stageBooks[i].data,
                stageBooks[i].isTarget,
                1000 + i);

            activeBooks.Add(book);
        }
    }

    private void NextStage()
    {
        currentStageIndex++;

        if (currentStageIndex >= Stages.Length)
        {
            Success();
            return;
        }

        CreateStage();
    }

    private bool ValidateSettings()
    {
        if (Stages == null || Stages.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogError("스테이지 데이터가 없습니다.");
#endif
            return false;
        }

        if (currentStageIndex < 0 ||
            currentStageIndex >= Stages.Length)
        {
#if UNITY_EDITOR
            Debug.LogError("잘못된 스테이지 인덱스입니다.");
#endif
            return false;
        }

        if (bookPrefab == null ||
            bookRoot == null ||
            pileCenter == null)
        {
#if UNITY_EDITOR
            Debug.LogError(
                "책 프리팹, BookRoot 또는 PileCenter가 " +
                "설정되지 않았습니다.");
#endif
            return false;
        }

        if (dropZone == null)
        {
#if UNITY_EDITOR
            Debug.LogError(
                "BookDropZone이 설정되지 않았습니다.");
#endif
            return false;
        }

        return true;
    }

    private BookData GetMatchingSimilarBook(
        BookData targetBook)
    {
        if (targetBook == null ||
            TargetBooks == null ||
            SimilarBooks == null)
        {
            return null;
        }

        for (int i = 0; i < TargetBooks.Length; i++)
        {
            BookData registeredTarget = TargetBooks[i];

            if (registeredTarget == null ||
                registeredTarget.sprite != targetBook.sprite)
            {
                continue;
            }

            if (i >= SimilarBooks.Length)
            {
                return null;
            }

            return SimilarBooks[i];
        }

        return null;
    }

    private List<BookData> GetUniqueRandomBooks(
        BookData[] books,
        int count,
        Sprite excludedSprite)
    {
        List<BookData> candidates =
            new List<BookData>();

        HashSet<Sprite> addedSprites =
            new HashSet<Sprite>();

        if (books == null)
        {
            return candidates;
        }

        for (int i = 0; i < books.Length; i++)
        {
            BookData book = books[i];

            if (book == null ||
                book.sprite == null ||
                book.sprite == excludedSprite ||
                !addedSprites.Add(book.sprite))
            {
                continue;
            }

            candidates.Add(book);
        }

        Shuffle(candidates);

        if (candidates.Count > count)
        {
            candidates.RemoveRange(
                count,
                candidates.Count - count);
        }

        return candidates;
    }

    private BookData GetRandomValidBook(
        BookData[] books)
    {
        if (books == null || books.Length == 0)
        {
            return null;
        }

        int startIndex = Random.Range(0, books.Length);

        for (int i = 0; i < books.Length; i++)
        {
            int index =
                (startIndex + i) % books.Length;

            if (books[index] != null)
            {
                return books[index];
            }
        }

        return null;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            (list[i], list[randomIndex]) =
                (list[randomIndex], list[i]);
        }
    }

    private void ClearActiveBooks()
    {
        for (int i = 0; i < activeBooks.Count; i++)
        {
            if (activeBooks[i] != null)
            {
                Destroy(activeBooks[i].gameObject);
            }
        }

        activeBooks.Clear();
    }

    private void ClearAllBooks()
    {
        ClearActiveBooks();

        for (int i = 0; i < placedBooks.Count; i++)
        {
            if (placedBooks[i] != null)
            {
                Destroy(placedBooks[i].gameObject);
            }
        }

        placedBooks.Clear();
    }

    protected override void OnEnd()
    {
        ClearAllBooks();

#if UNITY_EDITOR
        Debug.Log("책 찾기 미니게임 종료");
#endif
    }
}
