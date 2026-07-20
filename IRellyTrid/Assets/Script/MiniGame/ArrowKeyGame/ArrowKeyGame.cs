using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ArrowKeyGame : MiniGameBase
{
    [Header("Arrow Sprites")]
    [SerializeField] private Sprite rightArrow;
    [SerializeField] private Sprite leftArrow;
    [SerializeField] private Sprite downArrow;
    [SerializeField] private Sprite upArrow;

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

    private RectTransform boardRoot;
    private TMP_Text mistakeText;
    private TMP_Text resultText;

    private Vector2Int activeBoardSize;
    private int activeAllowedMistakes;
    private int currentInputIndex;
    private int mistakeCount;

    protected override void OnStart()
    {
        currentDifficulty =
            DayDifficultySelector.GetForDay(
                dayDifficulties,
                CurrentDay);

        if (!ValidateSettings())
        {
            Fail();
            return;
        }

        BuildInterface();
        ClearPathViews();

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
            mistakeCount++;
            pathViews[currentInputIndex].SetWrong();
            UpdateUI("X");

            if (mistakeCount >= activeAllowedMistakes)
            {
                Fail();
            }

            return;
        }

        pathViews[currentInputIndex].SetCompleted();
        currentInputIndex++;

        if (currentInputIndex >= pathDirections.Count)
        {
            goalView.SetGoalReached();
            UpdateUI("CLEAR");
            Success();
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
            upArrow == null)
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

        return true;
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
                return upArrow;

            case ArrowDirection.Down:
                return downArrow;

            case ArrowDirection.Left:
                return leftArrow;

            case ArrowDirection.Right:
                return rightArrow;

            default:
                return null;
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
            (gridPosition.x -
             (activeBoardSize.x - 1) * 0.5f) *
            currentDifficulty.cellSpacing.x,
            (gridPosition.y -
             (activeBoardSize.y - 1) * 0.5f) *
            currentDifficulty.cellSpacing.y);
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
        boardRoot.sizeDelta = new Vector2(1000f, 650f);
        boardRoot.anchoredPosition = new Vector2(0f, -20f);

        mistakeText = CreateText(
            "MistakeText",
            canvasObject.transform,
            new Vector2(0.5f, 1f),
            new Vector2(260f, 60f),
            30f);

        mistakeText.rectTransform.anchoredPosition =
            new Vector2(-170f, -70f);

        resultText = CreateText(
            "ResultText",
            canvasObject.transform,
            new Vector2(0.5f, 1f),
            new Vector2(260f, 60f),
            34f);

        resultText.rectTransform.anchoredPosition =
            new Vector2(170f, -70f);
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
                $"Mistakes {mistakeCount} / " +
                $"{activeAllowedMistakes}";
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
