using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TypingWordGame : MiniGameBase
{
    [Header("UI")]
    [SerializeField] private Image promptBackground;
    [SerializeField] private Image inputBackground;
    [SerializeField] private Sprite basePromptSprite;
    [SerializeField] private Sprite firstPromptSprite;
    [SerializeField] private Sprite secondPromptSprite;
    [SerializeField] private Sprite baseInputSprite;
    [SerializeField] private Sprite firstInputSprite;
    [SerializeField] private Sprite secondInputSprite;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_InputField inputField;

    [Header("Day Difficulties")]
    [SerializeField]
    private TypingWordDayDifficulty[] dayDifficulties;

    private readonly List<string> quizWords =
        new List<string>();

    private TypingWordDayDifficulty currentDifficulty;
    private int activeQuestionCount;
    private int activeAllowedMistakes;
    private int currentQuestionIndex;
    private int mistakeCount;
    private string currentAnswer;

    protected override void OnStart()
    {
        ApplyVariantVisuals();

        currentDifficulty =
            DayDifficultySelector.GetForDay(
                dayDifficulties,
                DifficultyDay);

        if (!ValidateDifficulty())
        {
            Fail();
            return;
        }

        currentQuestionIndex = 0;
        mistakeCount = 0;
        activeAllowedMistakes = Mathf.Max(
            1,
            currentDifficulty.allowedMistakes);

        if (resultText != null)
        {
            resultText.text = "";
        }

        CreateQuizWords();

        if (quizWords.Count == 0)
        {
#if UNITY_EDITOR
            Debug.LogError("사용 가능한 단어가 없습니다.");
#endif
            Fail();
            return;
        }

        GenerateQuestion();

        if (inputField != null)
        {
            inputField.onValidateInput -= ValidateTypingInput;
            inputField.onValidateInput += ValidateTypingInput;
            inputField.text = "";
            inputField.ActivateInputField();
        }

#if UNITY_EDITOR
        Debug.Log(
            $"단어 맞추기 미니게임 시작 / {CurrentDay}일차");
#endif
    }

    private void Update()
    {
        if (!IsPlaying || inputField == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitAnswer();
        }
    }

    private bool ValidateDifficulty()
    {
        if (currentDifficulty == null)
        {
#if UNITY_EDITOR
            Debug.LogError("타이핑 게임 일차 설정이 없습니다.");
#endif
            return false;
        }

        if (currentDifficulty.words == null ||
            currentDifficulty.words.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogError("등록된 단어가 없습니다.");
#endif
            return false;
        }

        return true;
    }

    private void CreateQuizWords()
    {
        quizWords.Clear();

        for (int i = 0;
             i < currentDifficulty.words.Length;
             i++)
        {
            string word = currentDifficulty.words[i];

            if (!string.IsNullOrWhiteSpace(word))
            {
                quizWords.Add(word.Trim());
            }
        }

        for (int i = quizWords.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            (quizWords[i], quizWords[randomIndex]) =
                (quizWords[randomIndex], quizWords[i]);
        }

        activeQuestionCount = Mathf.Min(
            Mathf.Max(1, currentDifficulty.totalQuestionCount),
            quizWords.Count);
    }

    private void GenerateQuestion()
    {
        currentAnswer = quizWords[currentQuestionIndex];

        if (wordText != null)
        {
            wordText.text = currentAnswer;
        }

        if (progressText != null)
        {
            progressText.text =
                $"{currentQuestionIndex + 1} / " +
                $"{activeQuestionCount}";
        }

        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }

#if UNITY_EDITOR
        Debug.Log($"정답 단어 : {currentAnswer}");
#endif
    }

    private void SubmitAnswer()
    {
        string playerInput = inputField.text.Trim();

        if (string.IsNullOrEmpty(playerInput))
        {
            return;
        }

        if (playerInput != currentAnswer)
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

            inputField.text = "";
            inputField.ActivateInputField();
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

        GenerateQuestion();
    }

    private void ApplyVariantVisuals()
    {
        (Sprite promptSprite, Sprite inputSprite) = AssetVariant switch
        {
            MiniGameAssetVariant.First =>
                (firstPromptSprite, firstInputSprite),
            MiniGameAssetVariant.Second =>
                (secondPromptSprite, secondInputSprite),
            _ => (basePromptSprite, baseInputSprite)
        };

        if (promptBackground != null && promptSprite != null)
            promptBackground.sprite = promptSprite;

        if (inputBackground != null && inputSprite != null)
            inputBackground.sprite = inputSprite;

        if (wordText != null)
            wordText.alignment = TextAlignmentOptions.Center;

        if (inputField == null)
            return;

        if (inputField.textViewport != null)
        {
            RectTransform viewport = inputField.textViewport;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(12f, 10f);
            viewport.offsetMax = new Vector2(-12f, -10f);
        }

        if (inputField.textComponent != null)
        {
            inputField.textComponent.alignment =
                TextAlignmentOptions.Center;
        }

        if (inputField.placeholder is TMP_Text placeholderText)
            placeholderText.alignment = TextAlignmentOptions.Center;
    }

    private char ValidateTypingInput(
        string text,
        int characterIndex,
        char addedCharacter)
    {
        return addedCharacter == ' ' ? '\0' : addedCharacter;
    }

    protected override void OnEnd()
    {
        if (inputField != null)
        {
            inputField.onValidateInput -= ValidateTypingInput;
            inputField.DeactivateInputField();
        }

#if UNITY_EDITOR
        Debug.Log("단어 맞추기 미니게임 종료");
#endif
    }
}
