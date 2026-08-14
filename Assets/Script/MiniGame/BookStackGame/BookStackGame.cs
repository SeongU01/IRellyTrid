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

    private readonly List<BookAssetVariant> availableAssetVariants =
        new List<BookAssetVariant>();

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

    CollectAvailableAssetVariants(
        remainingTargetBooks,
        availableAssetVariants);

    BookAssetVariant selectedVariant =
        availableAssetVariants[
            Random.Range(0, availableAssetVariants.Count)];

    int selectedIndex = GetRandomBookIndexForVariant(
        remainingTargetBooks,
        selectedVariant,
        null);
    BookData targetBook = remainingTargetBooks[selectedIndex];

    remainingTargetBooks.RemoveAt(selectedIndex);
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

        bool requiresUniqueBooks =
            CurrentDay >= 6 &&
            CurrentDay <= 9 &&
            AssetVariant == MiniGameAssetVariant.Second;

        if (requiresUniqueBooks)
        {
            HashSet<Sprite> usedSprites =
                new HashSet<Sprite> { targetBook.sprite };
            List<BookData> uniqueSimilarBooks =
                GetUniqueMatchingSimilarBooks(
                    targetBook,
                    similarBookCount,
                    usedSprites);

            for (int i = 0; i < uniqueSimilarBooks.Count; i++)
            {
                stageBooks.Add((uniqueSimilarBooks[i], false));
            }

            normalBookCount = bookCount - stageBooks.Count;
            List<BookData> uniqueNormalBooks =
                GetRandomUniqueBooks(
                    NormalBooks,
                    normalBookCount,
                    usedSprites);

            for (int i = 0; i < uniqueNormalBooks.Count; i++)
            {
                stageBooks.Add((uniqueNormalBooks[i], false));
            }
        }
        else
        {
            for (int i = 0; i < similarBookCount; i++)
            {
                CollectAvailableAssetVariants(
                    SimilarBooks,
                    availableAssetVariants);
                BookAssetVariant selectedVariant =
                    availableAssetVariants[
                        Random.Range(0, availableAssetVariants.Count)];
                BookData selectedSimilarBook =
                    GetMatchingSimilarBook(
                        targetBook,
                        selectedVariant);

                stageBooks.Add((selectedSimilarBook, false));
            }

            List<BookData> selectedNormalBooks =
                GetRandomBooksAllowingRepeats(
                    NormalBooks,
                    normalBookCount,
                    targetBook.sprite,
                    matchingSimilarBook.sprite);

            for (int i = 0; i < selectedNormalBooks.Count; i++)
            {
                stageBooks.Add((selectedNormalBooks[i], false));
            }
        }

        if (stageBooks.Count < bookCount)
        {
#if UNITY_EDITOR
            Debug.LogError(
                $"사용 가능한 서로 다른 책이 부족합니다. " +
                $"필요: {bookCount}, " +
                $"사용 가능: {stageBooks.Count}");
#endif
            Fail();
            return;
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
            ShowWrongFeedback();

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

        ShowCorrectFeedback();
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
        return GetMatchingSimilarBook(
            targetBook,
            targetBook != null
                ? targetBook.assetVariant
                : BookAssetVariant.Base);
    }

    private BookData GetMatchingSimilarBook(
        BookData targetBook,
        BookAssetVariant desiredVariant)
    {
        if (targetBook == null ||
            TargetBooks == null ||
            SimilarBooks == null)
        {
            return null;
        }

        int targetVariantIndex = 0;

        for (int i = 0; i < TargetBooks.Length; i++)
        {
            BookData registeredTarget = TargetBooks[i];

            if (registeredTarget == null ||
                registeredTarget.assetVariant !=
                targetBook.assetVariant)
            {
                continue;
            }

            if (registeredTarget.sprite == targetBook.sprite)
                break;

            targetVariantIndex++;
        }

        int desiredVariantCount = 0;

        for (int i = 0; i < SimilarBooks.Length; i++)
        {
            BookData similarBook = SimilarBooks[i];

            if (similarBook != null &&
                similarBook.assetVariant == desiredVariant)
            {
                desiredVariantCount++;
            }
        }

        if (desiredVariantCount == 0)
            return null;

        int desiredVariantIndex =
            targetVariantIndex % desiredVariantCount;

        for (int i = 0; i < SimilarBooks.Length; i++)
        {
            BookData similarBook = SimilarBooks[i];

            if (similarBook == null ||
                similarBook.assetVariant != desiredVariant)
            {
                continue;
            }

            if (desiredVariantIndex == 0)
                return similarBook;

            desiredVariantIndex--;
        }

        return null;
    }

    private List<BookData> GetRandomBooksAllowingRepeats(
        BookData[] books,
        int count,
        Sprite excludedTargetSprite,
        Sprite excludedSimilarSprite)
    {
        List<BookData> candidates =
            new List<BookData>();
        List<BookData> selectedBooks =
            new List<BookData>(Mathf.Max(0, count));

        HashSet<Sprite> addedSprites =
            new HashSet<Sprite>();

        if (books == null || count <= 0)
        {
            return selectedBooks;
        }

        for (int i = 0; i < books.Length; i++)
        {
            BookData book = books[i];

            if (book == null ||
                book.sprite == null ||
                book.sprite == excludedTargetSprite ||
                book.sprite == excludedSimilarSprite ||
                !addedSprites.Add(book.sprite))
            {
                continue;
            }

            candidates.Add(book);
        }

        while (candidates.Count > 0 &&
               selectedBooks.Count < count)
        {
            CollectAvailableAssetVariants(
                candidates,
                availableAssetVariants);

            BookAssetVariant selectedVariant =
                availableAssetVariants[
                    Random.Range(0, availableAssetVariants.Count)];
            Sprite previousSprite = selectedBooks.Count > 0
                ? selectedBooks[selectedBooks.Count - 1].sprite
                : null;
            int selectedIndex = GetRandomBookIndexForVariant(
                candidates,
                selectedVariant,
                previousSprite);

            selectedBooks.Add(candidates[selectedIndex]);
        }

        return selectedBooks;
    }

    private List<BookData> GetUniqueMatchingSimilarBooks(
        BookData targetBook,
        int count,
        HashSet<Sprite> usedSprites)
    {
        List<BookData> candidates = new List<BookData>();
        HashSet<Sprite> candidateSprites =
            new HashSet<Sprite>();

        CollectAvailableAssetVariants(
            SimilarBooks,
            availableAssetVariants);

        for (int i = 0; i < availableAssetVariants.Count; i++)
        {
            BookData book = GetMatchingSimilarBook(
                targetBook,
                availableAssetVariants[i]);

            if (book != null &&
                book.sprite != null &&
                !usedSprites.Contains(book.sprite) &&
                candidateSprites.Add(book.sprite))
            {
                candidates.Add(book);
            }
        }

        Shuffle(candidates);

        if (candidates.Count > count)
        {
            candidates.RemoveRange(
                count,
                candidates.Count - count);
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            usedSprites.Add(candidates[i].sprite);
        }

        return candidates;
    }

    private List<BookData> GetRandomUniqueBooks(
        BookData[] books,
        int count,
        HashSet<Sprite> usedSprites)
    {
        List<BookData> candidates = new List<BookData>();
        List<BookData> selectedBooks =
            new List<BookData>(Mathf.Max(0, count));
        HashSet<Sprite> candidateSprites =
            new HashSet<Sprite>();

        if (books == null || count <= 0)
            return selectedBooks;

        for (int i = 0; i < books.Length; i++)
        {
            BookData book = books[i];

            if (book != null &&
                book.sprite != null &&
                !usedSprites.Contains(book.sprite) &&
                candidateSprites.Add(book.sprite))
            {
                candidates.Add(book);
            }
        }

        while (candidates.Count > 0 &&
               selectedBooks.Count < count)
        {
            CollectAvailableAssetVariants(
                candidates,
                availableAssetVariants);
            BookAssetVariant selectedVariant =
                availableAssetVariants[
                    Random.Range(0, availableAssetVariants.Count)];
            int selectedIndex = GetRandomBookIndexForVariant(
                candidates,
                selectedVariant,
                null);
            BookData selectedBook = candidates[selectedIndex];

            selectedBooks.Add(selectedBook);
            usedSprites.Add(selectedBook.sprite);
            candidates.RemoveAt(selectedIndex);
        }

        return selectedBooks;
    }

    private static void CollectAvailableAssetVariants(
        List<BookData> books,
        List<BookAssetVariant> variants)
    {
        variants.Clear();

        for (int i = 0; i < books.Count; i++)
        {
            BookData book = books[i];

            if (book != null &&
                !variants.Contains(book.assetVariant))
            {
                variants.Add(book.assetVariant);
            }
        }
    }

    private static void CollectAvailableAssetVariants(
        BookData[] books,
        List<BookAssetVariant> variants)
    {
        variants.Clear();

        if (books == null)
            return;

        for (int i = 0; i < books.Length; i++)
        {
            BookData book = books[i];

            if (book != null &&
                !variants.Contains(book.assetVariant))
            {
                variants.Add(book.assetVariant);
            }
        }
    }

    private static int GetRandomBookIndexForVariant(
        List<BookData> books,
        BookAssetVariant variant,
        Sprite excludedPreviousSprite)
    {
        bool hasAlternative = HasAlternativeSprite(
            books,
            variant,
            excludedPreviousSprite);
        int selectedIndex = -1;
        int matchingCount = 0;

        for (int i = 0; i < books.Count; i++)
        {
            BookData book = books[i];

            if (book == null ||
                book.assetVariant != variant ||
                (hasAlternative &&
                 book.sprite == excludedPreviousSprite))
            {
                continue;
            }

            matchingCount++;

            if (Random.Range(0, matchingCount) == 0)
                selectedIndex = i;
        }

        return selectedIndex;
    }

    private static bool HasAlternativeSprite(
        List<BookData> books,
        BookAssetVariant variant,
        Sprite excludedSprite)
    {
        if (excludedSprite == null)
            return false;

        for (int i = 0; i < books.Count; i++)
        {
            BookData book = books[i];

            if (book != null &&
                book.assetVariant == variant &&
                book.sprite != excludedSprite)
            {
                return true;
            }
        }

        return false;
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
