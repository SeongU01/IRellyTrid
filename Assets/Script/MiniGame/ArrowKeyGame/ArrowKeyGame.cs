using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ArrowKeyGame : MiniGameBase
{
    [Header("Base Arrow Sprites")]
    [SerializeField] private Sprite rightArrow;
    [SerializeField] private Sprite leftArrow;
    [SerializeField] private Sprite downArrow;
    [SerializeField] private Sprite upArrow;

    [Header("First Variant Arrow Sprites")]
    [SerializeField] private Sprite firstRightArrow;
    [SerializeField] private Sprite firstLeftArrow;
    [SerializeField] private Sprite firstDownArrow;
    [SerializeField] private Sprite firstUpArrow;

    [Header("Second Variant Arrow Sprites")]
    [SerializeField] private Sprite secondRightArrow;
    [SerializeField] private Sprite secondLeftArrow;
    [SerializeField] private Sprite secondDownArrow;
    [SerializeField] private Sprite secondUpArrow;

    [Header("Day Difficulties")]
    [SerializeField]
    private ArrowKeyDayDifficulty[] dayDifficulties;

    [Header("Cell Style")]
    [SerializeField] private Vector2 cellSize = new Vector2(76f, 76f);
    [SerializeField] private Color normalColor = new Color(0.15f, 0.2f, 0.3f, 0.72f);
    [SerializeField] private Color activeColor = new Color(1f, 0.76f, 0.1f, 0.9f);
    [SerializeField] private Color completedColor = new Color(0.15f, 0.75f, 0.3f, 0.85f);
    [SerializeField] private Color wrongColor = new Color(0.9f, 0.18f, 0.18f, 0.9f);
    [SerializeField] private Color goalColor = new Color(0.2f, 0.55f, 1f, 0.9f);

    [Header("Path Style")]
    [SerializeField] private float pathLineThickness = 10f;
    [SerializeField] private Color pathLineColor = new Color(0.65f, 0.75f, 0.9f, 0.8f);

    [Header("Game Area")]
    [SerializeField] private Vector2 gameAreaCenter =
        new Vector2(-165f, -150f);
    [SerializeField] private Vector2 gameAreaSize =
        new Vector2(860f, 700f);
    [SerializeField] private float gameAreaPadding = 24f;
    [SerializeField] private Color uiTextColor =
        new Color(0.12f, 0.12f, 0.12f, 1f);

    private readonly List<Vector2Int> pathPositions =
        new List<Vector2Int>();

    private readonly List<ArrowDirection> pathDirections =
        new List<ArrowDirection>();

    private readonly List<ArrowDirection> noteDirections =
        new List<ArrowDirection>();

    private readonly List<ArrowPathCellView> pathViews =
        new List<ArrowPathCellView>();

    private readonly List<GameObject> pathLines =
        new List<GameObject>();

    private readonly HashSet<Vector2Int> visitedPositions =
        new HashSet<Vector2Int>();

    private ArrowKeyDayDifficulty currentDifficulty;
    private ArrowPathCellView goalView;
    private Vector2 pathGridCenter;

    private RectTransform boardRoot;
    private TMP_Text mistakeText;
    private TMP_Text resultText;

    private Vector2Int activeBoardSize;
    private int activeStageCount;
    private int activeAllowedMistakes;
    private int currentStageIndex;
    private int currentInputIndex;
    private int mistakeCount;

    protected override void OnStart()
    {
        currentDifficulty =
            DayDifficultySelector.GetForDay(
                dayDifficulties,
                DifficultyDay);

        if (!ValidateSettings())
        {
            Fail();
            return;
        }

        BuildInterface();
        ClearPathViews();

        currentStageIndex = 0;
        currentInputIndex = 0;
        mistakeCount = 0;

        if (!CreateRandomPath())
        {
#if UNITY_EDITOR
            Debug.LogError("방향키 경로 생성에 실패했습니다.");
#endif
            Fail();
            return;
        }

        CalculatePathLayout();
        CreateRandomNotes();
        CreatePathViews();
        UpdateUI("");

#if UNITY_EDITOR
        Debug.Log(
            $"방향키 미니게임 시작 / {CurrentDay}일차 / " +
            $"방향키 {pathDirections.Count}개");
#endif
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        ArrowDirection? pressedDirection =
            GetPressedDirection();

        if (pressedDirection.HasValue)
        {
            SubmitDirection(pressedDirection.Value);
        }
    }

    private ArrowDirection? GetPressedDirection()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return null;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame)
            return ArrowDirection.Up;

        if (keyboard.downArrowKey.wasPressedThisFrame)
            return ArrowDirection.Down;

        if (keyboard.leftArrowKey.wasPressedThisFrame)
            return ArrowDirection.Left;

        if (keyboard.rightArrowKey.wasPressedThisFrame)
            return ArrowDirection.Right;

        return null;
    }

    private void SubmitDirection(ArrowDirection inputDirection)
    {
        ArrowDirection answer =
            noteDirections[currentInputIndex];

        if (inputDirection != answer)
        {
            ShowWrongFeedback();
            mistakeCount++;
            pathViews[currentInputIndex].SetWrong();
            UpdateUI("X");

            if (mistakeCount >= activeAllowedMistakes)
            {
                Fail();
            }

            return;
        }

        ShowCorrectFeedback();
        pathViews[currentInputIndex].SetCompleted();
        currentInputIndex++;

        if (currentInputIndex >= pathDirections.Count)
        {
            goalView.SetGoalReached();
            currentStageIndex++;

            if (currentStageIndex >= activeStageCount)
            {
                UpdateUI("CLEAR");
                Success();
                return;
            }

            BeginNextStage();
            return;
        }

        pathViews[currentInputIndex].SetActiveState();
        UpdateUI("O");
    }

    private bool ValidateSettings()
    {
        if (currentDifficulty == null)
        {
#if UNITY_EDITOR
            Debug.LogError("방향키 게임 일차 설정이 없습니다.");
#endif
            return false;
        }

        if (rightArrow == null ||
            leftArrow == null ||
            downArrow == null ||
            upArrow == null ||
            firstRightArrow == null ||
            firstLeftArrow == null ||
            firstDownArrow == null ||
            firstUpArrow == null ||
            secondRightArrow == null ||
            secondLeftArrow == null ||
            secondDownArrow == null ||
            secondUpArrow == null)
        {
#if UNITY_EDITOR
            Debug.LogError("방향키 스프라이트가 모두 연결되지 않았습니다.");
#endif
            return false;
        }

        activeBoardSize = new Vector2Int(
            Mathf.Max(1, currentDifficulty.boardSize.x),
            Mathf.Max(1, currentDifficulty.boardSize.y));

        if (activeBoardSize.x * activeBoardSize.y < 2)
        {
#if UNITY_EDITOR
            Debug.LogError("보드는 최소 두 칸 이상이어야 합니다.");
#endif
            return false;
        }

        activeAllowedMistakes = Mathf.Max(
            1,
            currentDifficulty.allowedMistakes);
        activeStageCount = Mathf.Max(1, currentDifficulty.stageCount);

        return true;
    }

    private void BeginNextStage()
    {
        ClearPathViews();
        currentInputIndex = 0;

        if (!CreateRandomPath())
        {
#if UNITY_EDITOR
            Debug.LogError("Failed to create an arrow path.");
#endif
            Fail();
            return;
        }

        CalculatePathLayout();
        CreateRandomNotes();
        CreatePathViews();
        UpdateUI("");
        RequestTimerReset();
    }

    private bool CreateRandomPath()
    {
        int maximumArrowCount =
            activeBoardSize.x * activeBoardSize.y - 1;

        int minimumArrowCount = Mathf.Clamp(
            Mathf.Min(
                currentDifficulty.minArrowCount,
                currentDifficulty.maxArrowCount),
            1,
            maximumArrowCount);

        int configuredMaximum = Mathf.Clamp(
            Mathf.Max(
                currentDifficulty.minArrowCount,
                currentDifficulty.maxArrowCount),
            minimumArrowCount,
            maximumArrowCount);

        int arrowCount = Random.Range(
            minimumArrowCount,
            configuredMaximum + 1);

        int requiredTurnCount = Mathf.Clamp(
            currentDifficulty.minTurnCount,
            0,
            arrowCount - 1);

        for (int attempt = 0; attempt < 100; attempt++)
        {
            pathPositions.Clear();
            pathDirections.Clear();
            visitedPositions.Clear();

            Vector2Int startPosition = new Vector2Int(
                Random.Range(0, activeBoardSize.x),
                Random.Range(0, activeBoardSize.y));

            pathPositions.Add(startPosition);
            visitedPositions.Add(startPosition);

            if (TryExtendPath(startPosition, arrowCount) &&
                CountTurns() >= requiredTurnCount)
            {
                return true;
            }
        }

        return false;
    }

    private int CountTurns()
    {
        int turnCount = 0;

        for (int i = 1; i < pathDirections.Count; i++)
        {
            if (pathDirections[i] != pathDirections[i - 1])
            {
                turnCount++;
            }
        }

        return turnCount;
    }

    private bool TryExtendPath(
        Vector2Int currentPosition,
        int remainingArrowCount)
    {
        if (remainingArrowCount <= 0)
        {
            return true;
        }

        List<ArrowDirection> directions =
            CreateShuffledDirections();

        for (int i = 0; i < directions.Count; i++)
        {
            ArrowDirection direction = directions[i];

            Vector2Int nextPosition =
                currentPosition + GetOffset(direction);

            if (!CanVisit(nextPosition, currentPosition))
            {
                continue;
            }

            visitedPositions.Add(nextPosition);
            pathPositions.Add(nextPosition);
            pathDirections.Add(direction);

            if (TryExtendPath(
                    nextPosition,
                    remainingArrowCount - 1))
            {
                return true;
            }

            pathDirections.RemoveAt(
                pathDirections.Count - 1);

            pathPositions.RemoveAt(
                pathPositions.Count - 1);

            visitedPositions.Remove(nextPosition);
        }

        return false;
    }

    private bool CanVisit(
        Vector2Int position,
        Vector2Int previousPosition)
    {
        if (position.x < 0 ||
            position.y < 0 ||
            position.x >= activeBoardSize.x ||
            position.y >= activeBoardSize.y ||
            visitedPositions.Contains(position))
        {
            return false;
        }

        ArrowDirection[] directions =
        {
            ArrowDirection.Up,
            ArrowDirection.Down,
            ArrowDirection.Left,
            ArrowDirection.Right
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int neighbor =
                position + GetOffset(directions[i]);

            if (neighbor != previousPosition &&
                visitedPositions.Contains(neighbor))
            {
                return false;
            }
        }

        return true;
    }

    private List<ArrowDirection> CreateShuffledDirections()
    {
        List<ArrowDirection> directions =
            new List<ArrowDirection>
            {
                ArrowDirection.Up,
                ArrowDirection.Down,
                ArrowDirection.Left,
                ArrowDirection.Right
            };

        for (int i = directions.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            (directions[i], directions[randomIndex]) =
                (directions[randomIndex], directions[i]);
        }

        return directions;
    }

    private void CreateRandomNotes()
    {
        noteDirections.Clear();

        for (int i = 0; i < pathDirections.Count; i++)
        {
            List<ArrowDirection> candidates =
                CreateShuffledDirections();

            if (i > 0)
            {
                candidates.Remove(noteDirections[i - 1]);
            }

            noteDirections.Add(candidates[0]);
        }
    }

    private Vector2Int GetOffset(ArrowDirection direction)
    {
        switch (direction)
        {
            case ArrowDirection.Up:
                return Vector2Int.up;

            case ArrowDirection.Down:
                return Vector2Int.down;

            case ArrowDirection.Left:
                return Vector2Int.left;

            case ArrowDirection.Right:
                return Vector2Int.right;

            default:
                return Vector2Int.zero;
        }
    }

    private Sprite GetArrowSprite(ArrowDirection direction)
    {
        switch (direction)
        {
            case ArrowDirection.Up:
                return SelectVariantSprite(
                    upArrow,
                    firstUpArrow,
                    secondUpArrow);

            case ArrowDirection.Down:
                return SelectVariantSprite(
                    downArrow,
                    firstDownArrow,
                    secondDownArrow);

            case ArrowDirection.Left:
                return SelectVariantSprite(
                    leftArrow,
                    firstLeftArrow,
                    secondLeftArrow);

            case ArrowDirection.Right:
                return SelectVariantSprite(
                    rightArrow,
                    firstRightArrow,
                    secondRightArrow);

            default:
                return null;
        }
    }

    private Sprite SelectVariantSprite(
        Sprite baseSprite,
        Sprite firstVariantSprite,
        Sprite secondVariantSprite)
    {
        int variantCount = 1;

        if (DifficultyDay >= 6)
        {
            variantCount = 3;
        }
        else if (DifficultyDay >= 5)
        {
            variantCount = 2;
        }

        switch (Random.Range(0, variantCount))
        {
            case 1:
                return firstVariantSprite;

            case 2:
                return secondVariantSprite;

            default:
                return baseSprite;
        }
    }

    private void CreatePathViews()
    {
        CreatePathLines();

        for (int i = 0; i < noteDirections.Count; i++)
        {
            ArrowPathCellView cell = CreateCell(
                $"Arrow_{i + 1}",
                pathPositions[i]);

            cell.ShowArrow(
                GetArrowSprite(noteDirections[i]),
                i == 0);

            pathViews.Add(cell);
        }

        goalView = CreateCell(
            "Goal",
            pathPositions[pathPositions.Count - 1]);

        goalView.ShowGoal();
        pathViews[0].SetActiveState();
    }

    private void CreatePathLines()
    {
        for (int i = 0; i < pathPositions.Count - 1; i++)
        {
            Vector2 start = GetBoardPosition(pathPositions[i]);
            Vector2 end = GetBoardPosition(pathPositions[i + 1]);
            Vector2 difference = end - start;

            GameObject lineObject = new GameObject(
                $"PathLine_{i + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            RectTransform lineRect =
                lineObject.GetComponent<RectTransform>();

            lineRect.SetParent(boardRoot, false);
            lineRect.anchoredPosition = (start + end) * 0.5f;
            lineRect.sizeDelta = new Vector2(
                difference.magnitude,
                Mathf.Max(1f, pathLineThickness));
            lineRect.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(difference.y, difference.x) *
                Mathf.Rad2Deg);

            Image lineImage = lineObject.GetComponent<Image>();
            lineImage.color = pathLineColor;
            lineImage.raycastTarget = false;

            pathLines.Add(lineObject);
        }
    }

    private ArrowPathCellView CreateCell(
        string cellName,
        Vector2Int gridPosition)
    {
        GameObject cellObject = new GameObject(
            cellName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(ArrowPathCellView));

        RectTransform cellRect =
            cellObject.GetComponent<RectTransform>();

        cellRect.SetParent(boardRoot, false);
        cellRect.sizeDelta = cellSize;

        cellRect.anchoredPosition = GetBoardPosition(gridPosition);

        Image background = cellObject.GetComponent<Image>();
        background.raycastTarget = false;

        GameObject arrowObject = new GameObject(
            "ArrowImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform arrowRect =
            arrowObject.GetComponent<RectTransform>();

        arrowRect.SetParent(cellRect, false);
        arrowRect.anchorMin = Vector2.zero;
        arrowRect.anchorMax = Vector2.one;
        arrowRect.offsetMin = new Vector2(7f, 7f);
        arrowRect.offsetMax = new Vector2(-7f, -7f);

        Image arrowImage = arrowObject.GetComponent<Image>();
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;

        TMP_Text marker = CreateText(
            "MarkerText",
            cellRect,
            Vector2.zero,
            new Vector2(100f, 28f),
            17f);

        marker.rectTransform.anchoredPosition =
            new Vector2(0f, cellSize.y * 0.64f);

        ArrowPathCellView view =
            cellObject.GetComponent<ArrowPathCellView>();

        view.Initialize(
            background,
            arrowImage,
            marker,
            normalColor,
            activeColor,
            completedColor,
            wrongColor,
            goalColor);

        return view;
    }

    private Vector2 GetBoardPosition(Vector2Int gridPosition)
    {
        return new Vector2(
            (gridPosition.x - pathGridCenter.x) *
            currentDifficulty.cellSpacing.x,
            (gridPosition.y - pathGridCenter.y) *
            currentDifficulty.cellSpacing.y);
    }

    private void CalculatePathLayout()
    {
        if (pathPositions.Count == 0 || boardRoot == null)
        {
            return;
        }

        int minX = pathPositions[0].x;
        int maxX = pathPositions[0].x;
        int minY = pathPositions[0].y;
        int maxY = pathPositions[0].y;

        for (int i = 1; i < pathPositions.Count; i++)
        {
            Vector2Int position = pathPositions[i];
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxY = Mathf.Max(maxY, position.y);
        }

        pathGridCenter = new Vector2(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f);

        float contentWidth =
            (maxX - minX) * currentDifficulty.cellSpacing.x +
            cellSize.x;

        float contentHeight =
            (maxY - minY) * currentDifficulty.cellSpacing.y +
            cellSize.y;

        float availableWidth = Mathf.Max(
            1f,
            gameAreaSize.x - gameAreaPadding * 2f);

        float availableHeight = Mathf.Max(
            1f,
            gameAreaSize.y - gameAreaPadding * 2f);

        float layoutScale = Mathf.Min(
            1f,
            availableWidth / Mathf.Max(1f, contentWidth),
            availableHeight / Mathf.Max(1f, contentHeight));

        boardRoot.localScale =
            new Vector3(layoutScale, layoutScale, 1f);
    }

    private void BuildInterface()
    {
        if (boardRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.matchWidthOrHeight = 0.5f;

        GameObject boardObject = new GameObject(
            "BoardRoot",
            typeof(RectTransform));

        boardRoot = boardObject.GetComponent<RectTransform>();
        boardRoot.SetParent(canvasObject.transform, false);
        boardRoot.anchorMin = new Vector2(0.5f, 0.5f);
        boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
        boardRoot.sizeDelta = gameAreaSize;
        boardRoot.anchoredPosition = gameAreaCenter;

        mistakeText = CreateText(
            "MistakeText",
            canvasObject.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(260f, 60f),
            30f);

        mistakeText.rectTransform.anchoredPosition =
            gameAreaCenter +
            new Vector2(-220f, gameAreaSize.y * 0.5f - 38f);
        mistakeText.color = uiTextColor;

        resultText = CreateText(
            "ResultText",
            canvasObject.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(260f, 60f),
            34f);

        resultText.rectTransform.anchoredPosition =
            gameAreaCenter +
            new Vector2(220f, gameAreaSize.y * 0.5f - 38f);
        resultText.color = uiTextColor;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 size,
        float fontSize)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.SetParent(parent, false);
        textRect.anchorMin = anchor;
        textRect.anchorMax = anchor;
        textRect.sizeDelta = size;

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();

        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private void UpdateUI(string result)
    {
        if (mistakeText != null)
        {
            mistakeText.text =
                $"Stage {currentStageIndex + 1} / {activeStageCount}    " +
                $"Mistakes {mistakeCount} / {activeAllowedMistakes}";
        }

        if (resultText != null)
        {
            resultText.text = result;
        }
    }

    private void ClearPathViews()
    {
        for (int i = 0; i < pathViews.Count; i++)
        {
            if (pathViews[i] != null)
            {
                Destroy(pathViews[i].gameObject);
            }
        }

        pathViews.Clear();

        for (int i = 0; i < pathLines.Count; i++)
        {
            if (pathLines[i] != null)
            {
                Destroy(pathLines[i]);
            }
        }

        pathLines.Clear();
        noteDirections.Clear();

        if (goalView != null)
        {
            Destroy(goalView.gameObject);
            goalView = null;
        }
    }

    protected override void OnEnd()
    {
        ClearPathViews();

#if UNITY_EDITOR
        Debug.Log("방향키 미니게임 종료");
#endif
    }
}
