using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class BookStackGame : MiniGameBase
{
    [Header("UI")]
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text resultText;

    [Header("Book Spawn")]
    [SerializeField] private Transform bookRoot;
    [SerializeField] private BookItem bookPrefab;
    [SerializeField] private float bookYOffset = 0.35f;
    [SerializeField] private float bookXOffset = 0.08f;

    [Header("Stage Settings")]
    [SerializeField]
    private BookStageData[] stages =
    {
        new BookStageData { bookCount = 4, similarBookCount =0},
        new BookStageData { bookCount = 5, similarBookCount =2},
        new BookStageData { bookCount = 6, similarBookCount =3},
        new BookStageData { bookCount = 7, similarBookCount =4},
        new BookStageData { bookCount = 8, similarBookCount =5},
    };

    [Header("Book Data")]
    [SerializeField] private BookData[] targetBooks;
    [SerializeField] private BookData[] similarBooks;
    [SerializeField] private BookData[] normalBooks;

    private int currentStageIndex;
    private readonly List<BookItem> spawnedBooks = new List<BookItem>();

    protected override void OnStart()
    {
        currentStageIndex = 0;
        if(resultText != null) 
        {
            resultText.text = "";
        }

        CreateStage();
    }

    private void CreateStage()
    {
        ClearBooks();

        if(stages == null || stages.Length ==0)
        {
#if UNITY_EDITOR
            Debug.LogError("스테이지 데이터가 없습니다.");
#endif
            Fail();
            return;
        }

        if(bookPrefab == null || bookRoot == null)
        {
#if UNITY_EDITOR
            Debug.LogError("책 프리팹 또는 루트가 설정되지 않았습니다.");
#endif
            Fail();
            return;
        }
        
        if (targetBooks == null || targetBooks.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogError("타겟 책 데이터가 없습니다.");
#endif
            Fail();
            return;
        }

        BookStageData stage = stages[currentStageIndex];

        int bookCount = Mathf.Max(1, stage.bookCount);
        int similarBookCount = Mathf.Clamp(stage.similarBookCount, 0, bookCount - 1);
        int normalBookCount = bookCount - similarBookCount - 1;

        BookData targetBook = GetRandomBook(targetBooks);

        List<(BookData data,bool isTarget)> stageBooks = new List<(BookData, bool)>();

        stageBooks.Add((targetBook, true));

        for (int i = 0; i < similarBookCount; i++)
        {
            BookData similarBook = GetRandomBook(similarBooks);
        
            if(similarBook == null)
            {
                similarBook = targetBook;
            }

            stageBooks.Add((similarBook, false));
        }

        for(int i = 0; i < normalBookCount; i++)
        {
            BookData normalBook = GetRandomBook(normalBooks);
                    
            if(normalBook == null)
            {
                normalBook = targetBook;
            }

            stageBooks.Add((normalBook, false));
        }

        Shuffle(stageBooks);
        SpawnBooks(stageBooks);

        if (targetText != null)
            targetText.text = $"Find: {targetBook.bookName}";

        if (progressText != null)
            progressText.text = $"{currentStageIndex + 1} / {stages.Length}";

#if UNITY_EDITOR
        Debug.Log($"Stage {currentStageIndex + 1} 시작 / 목표 책: {targetBook.bookName}");
#endif
    }

    public void OnBookClicked(BookItem clickedBook)
    {
        if (!IsPlaying)
            return;

        if (clickedBook == null)
            return;

        BookItem topBook = GetTopBook();

        if (clickedBook != topBook)
        {
            if (resultText != null)
                resultText.text = "X";

#if UNITY_EDITOR
            Debug.Log("위에 있는 책부터 치워야 합니다.");
#endif
            return;
        }

        if (clickedBook.IsTarget)
        {
            if (resultText != null)
                resultText.text = "O";

            NextStage();
        }
        else
        {
            RemoveBook(clickedBook);
        }
    }

    private BookItem GetTopBook()
    {
        if (spawnedBooks.Count == 0)
            return null;

        return spawnedBooks[spawnedBooks.Count - 1];
    }

    private void RemoveBook(BookItem book)
    {
        if (book == null)
            return;

        spawnedBooks.Remove(book);
        Destroy(book.gameObject);

        if (resultText != null)
            resultText.text = "";

#if UNITY_EDITOR
        Debug.Log($"책 제거: {book.BookName}");
#endif
    }

    private void NextStage()
    {
        currentStageIndex++;

        if (currentStageIndex >= stages.Length)
        {
            Success();
            return;
        }

        CreateStage();
    }
    private void SpawnBooks(List<(BookData data, bool isTarget)> stageBooks)
    {
        for (int i = 0; i < stageBooks.Count; i++)
        {
            Vector3 position = new Vector3(
                i * bookXOffset,
                i * bookYOffset,
                -i * 0.01f
            );

            BookItem book = Instantiate(bookPrefab, bookRoot);
            book.transform.localPosition = position;

            book.Init(this, stageBooks[i].data, stageBooks[i].isTarget, i);

            spawnedBooks.Add(book);
        }
    }

    private BookData GetRandomBook(BookData[] books)
    {
        if (books == null || books.Length==0)
        {
            return null;
        }
        return books[Random.Range(0, books.Length)];
    }

    private void Shuffle<T>(List<T> list)
    {
        for(int i = list.Count - 1; i>0;i--)
        {
            int randomIndex = Random.Range(0, i +1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
    protected override void OnEnd()
    {
        ClearBooks();

#if UNITY_EDITOR
        Debug.Log("책 무더기 미니게임 종료");
#endif
    }

    private void ClearBooks()
    {
        for (int i = 0; i < spawnedBooks.Count; i++)
        {
            if (spawnedBooks[i] != null)
            {
                Destroy(spawnedBooks[i].gameObject);
            }
        }
        spawnedBooks.Clear();
    }
}