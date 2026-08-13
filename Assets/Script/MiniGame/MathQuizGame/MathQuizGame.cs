using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MathQuizGame : MiniGameBase
{
    private static readonly Key[] NumberRowKeys =
    {
        Key.Digit0,
        Key.Digit1,
        Key.Digit2,
        Key.Digit3,
        Key.Digit4,
        Key.Digit5,
        Key.Digit6,
        Key.Digit7,
        Key.Digit8,
        Key.Digit9
    };

    private static readonly Key[] NumpadKeys =
    {
        Key.Numpad0,
        Key.Numpad1,
        Key.Numpad2,
        Key.Numpad3,
        Key.Numpad4,
        Key.Numpad5,
        Key.Numpad6,
        Key.Numpad7,
        Key.Numpad8,
        Key.Numpad9
    };

    [Header("UI")]
    [SerializeField] private Image noteImage;
    [SerializeField] private Sprite baseNoteSprite;
    [SerializeField] private Sprite firstVariantNoteSprite;
    [SerializeField] private Sprite secondVariantNoteSprite;
    [SerializeField] private Vector2 baseAnswerPosition =
        new Vector2(-272.58f, -279.58f);
    [SerializeField] private Vector2 firstVariantAnswerPosition =
        new Vector2(-262.21f, -260.06f);
    [SerializeField] private Vector2 secondVariantAnswerPosition =
        new Vector2(-261.74f, -260.78f);
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text progressText;
    [FormerlySerializedAs("inputText")]
    [SerializeField] private TMP_Text answerText;
    [SerializeField] private TMP_Text resultText;

    [Header("Day Difficulties")]
    [SerializeField]
    private MathQuizDayDifficulty[] dayDifficulties;

    private readonly List<int> availableOperations =
        new List<int>();

    private MathQuizDayDifficulty currentDifficulty;
    private int activeQuestionCount;
    private int activeAllowedMistakes;
    private int currentQuestionIndex;
    private int mistakeCount;
    private int correctAnswer;
    private string currentInput = "";

    protected override void OnStart()
    {
        ApplyNoteSprite();

        currentDifficulty =
            DayDifficultySelector.GetForDay(
                dayDifficulties,
                DifficultyDay);

        if (!ValidateDifficulty())
        {
            Fail();
            return;
        }

        CreateAvailableOperations();
        activeQuestionCount = Mathf.Max(
            1,
            currentDifficulty.totalQuestionCount);

        currentQuestionIndex = 0;
        mistakeCount = 0;
        activeAllowedMistakes = Mathf.Max(
            1,
            currentDifficulty.allowedMistakes);
        currentInput = "";

        if (resultText != null)
        {
            resultText.text = "";
        }

        GenerateQuestion();

#if UNITY_EDITOR
        Debug.Log(
            $"간단 연산 게임 시작 / {CurrentDay}일차");
#endif
    }

    private void ApplyNoteSprite()
    {
        if (noteImage == null)
            return;

        (Sprite selectedSprite, Vector2 answerPosition) = AssetVariant switch
        {
            MiniGameAssetVariant.First =>
                (firstVariantNoteSprite, firstVariantAnswerPosition),
            MiniGameAssetVariant.Second =>
                (secondVariantNoteSprite, secondVariantAnswerPosition),
            _ => (baseNoteSprite, baseAnswerPosition)
        };

        if (selectedSprite != null)
            noteImage.sprite = selectedSprite;

        if (answerText != null)
            answerText.rectTransform.anchoredPosition = answerPosition;
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        HandleNumberInput();
        HandleBackSpace();
        HandleSubmit();
    }

    private void HandleNumberInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("키보드 입력을 감지할 수 없습니다.");
#endif
            return;
        }

        for (int i = 0; i <= 9; i++)
        {
            if (keyboard[NumberRowKeys[i]].wasPressedThisFrame)
            {
                currentInput += i.ToString();
                UpdateInputText();
                return;
            }

            if (keyboard[NumpadKeys[i]].wasPressedThisFrame)
            {
                currentInput += i.ToString();
                UpdateInputText();
                return;
            }
        }
    }

    private void HandleBackSpace()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null ||
            !keyboard.backspaceKey.wasPressedThisFrame ||
            currentInput.Length <= 0)
        {
            return;
        }

        currentInput = currentInput.Substring(
            0,
            currentInput.Length - 1);

        UpdateInputText();
    }

    private void HandleSubmit()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null ||
            (!keyboard.enterKey.wasPressedThisFrame &&
             !keyboard.numpadEnterKey.wasPressedThisFrame) ||
            string.IsNullOrEmpty(currentInput))
        {
            return;
        }

        if (!int.TryParse(currentInput, out int playerAnswer))
        {
            currentInput = "";
            UpdateInputText();
            return;
        }

        if (playerAnswer != correctAnswer)
        {
            ShowWrongFeedback();
            mistakeCount++;

            if (resultText != null)
            {
                resultText.text =
                    $"X  ({mistakeCount}/{activeAllowedMistakes})";
            }

            if (mistakeCount >= activeAllowedMistakes)
            {
                Fail();
                return;
            }

            currentInput = "";
            UpdateInputText();
            return;
        }

        ShowCorrectFeedback();

        if (resultText != null)
        {
            resultText.text = "O";
        }

        currentQuestionIndex++;

        if (currentQuestionIndex >= activeQuestionCount)
        {
            Success();
            return;
        }

        currentInput = "";
        UpdateInputText();
        GenerateQuestion();
    }

    private void GenerateQuestion()
    {
        int operation = availableOperations[
            Random.Range(0, availableOperations.Count)];

        int minNumber = Mathf.Max(
            1,
            Mathf.Min(
                currentDifficulty.minNumber,
                currentDifficulty.maxNumber));

        int maxNumber = Mathf.Max(
            minNumber,
            Mathf.Max(
                currentDifficulty.minNumber,
                currentDifficulty.maxNumber));

        int left = 0;
        int right = 0;
        char operatorChar = '+';

        switch (operation)
        {
            case 0:
                left = Random.Range(minNumber, maxNumber + 1);
                right = Random.Range(minNumber, maxNumber + 1);
                correctAnswer = left + right;
                operatorChar = '+';
                break;

            case 1:
                left = Random.Range(minNumber, maxNumber + 1);
                right = Random.Range(minNumber, maxNumber + 1);

                if (left < right)
                {
                    (left, right) = (right, left);
                }

                correctAnswer = left - right;
                operatorChar = '-';
                break;

            case 2:
                left = Random.Range(minNumber, maxNumber + 1);
                right = Random.Range(minNumber, maxNumber + 1);
                correctAnswer = left * right;
                operatorChar = '×';
                break;

            case 3:
                right = Random.Range(minNumber, maxNumber + 1);
                correctAnswer = Random.Range(minNumber, maxNumber + 1);
                left = right * correctAnswer;
                operatorChar = '÷';
                break;
        }

        if (questionText != null)
        {
            questionText.text =
                $"{left} {operatorChar} {right} = ?";
        }

        if (progressText != null)
        {
            progressText.text =
                $"{currentQuestionIndex + 1} / " +
                $"{activeQuestionCount}";
        }

        UpdateInputText();

#if UNITY_EDITOR
        Debug.Log($"정답 : {correctAnswer}");
#endif
    }

    private void CreateAvailableOperations()
    {
        availableOperations.Clear();

        if (currentDifficulty.useAddition)
            availableOperations.Add(0);

        if (currentDifficulty.useSubtraction)
            availableOperations.Add(1);

        if (currentDifficulty.useMultiplication)
            availableOperations.Add(2);

        if (currentDifficulty.useDivision)
            availableOperations.Add(3);
    }

    private bool ValidateDifficulty()
    {
        if (currentDifficulty == null)
        {
#if UNITY_EDITOR
            Debug.LogError("수학 게임 일차 설정이 없습니다.");
#endif
            return false;
        }

        if (!currentDifficulty.useAddition &&
            !currentDifficulty.useSubtraction &&
            !currentDifficulty.useMultiplication &&
            !currentDifficulty.useDivision)
        {
#if UNITY_EDITOR
            Debug.LogError("사용할 연산자가 하나도 없습니다.");
#endif
            return false;
        }

        return true;
    }

    private void UpdateInputText()
    {
        if (answerText != null)
        {
            answerText.text = currentInput;
        }
    }

    protected override void OnEnd()
    {
#if UNITY_EDITOR
        Debug.Log("간단 연산 게임 종료");
#endif
    }
}
