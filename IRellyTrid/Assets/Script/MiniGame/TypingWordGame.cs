using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TypingWordGame : MiniGameBase
{
    [Header("UI")]
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_InputField inputField;

    [Header("Game Settings")]
    [SerializeField] private int totalQuestionCount = 10;

    [Header("Words")]
    [SerializeField] private string[] words;

    private int currentQuestionIndex;
    private string currentAnswer;

    private readonly List<string> quizWords = new List<string>();

    protected override void OnStart()
    {
        currentQuestionIndex = 0;

        if (resultText != null)
            resultText.text = "";

        CreateQuizWords();
        if (!IsPlaying)
            return;

        GenerateQuestion();

        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }

#if UNITY_EDITOR
        Debug.Log("단어 맞추기 미니게임 시작");
#endif
    }

    private void Update()
    {
        if (!IsPlaying)
            return;

        if (inputField == null)
            return;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitAnswer();
        }
    }

    private void CreateQuizWords()
    {
        quizWords.Clear();

        if (words == null || words.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogError("등록된 단어가 없습니다.");
#endif
            Fail();
            return;
        }

        quizWords.AddRange(words);

        for (int i = quizWords.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (quizWords[i], quizWords[randomIndex]) =
                (quizWords[randomIndex], quizWords[i]);
        }

        if (totalQuestionCount > quizWords.Count)
        {
#if UNITY_EDITOR
            Debug.LogWarning("문제 수가 등록된 단어 수보다 많습니다. 문제 수를 단어 수에 맞춥니다.");
#endif
            totalQuestionCount = quizWords.Count;
        }
    }

    private void GenerateQuestion()
    {
        currentAnswer = quizWords[currentQuestionIndex];

        if (wordText != null)
            wordText.text = currentAnswer;

        if (progressText != null)
            progressText.text = $"{currentQuestionIndex + 1} / {totalQuestionCount}";

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
            return;

        if (playerInput == currentAnswer)
        {
            if (resultText != null)
                resultText.text = "O";

            currentQuestionIndex++;

            if (currentQuestionIndex >= totalQuestionCount)
            {
                Success();
                return;
            }

            GenerateQuestion();
        }
        else
        {
            if (resultText != null)
                resultText.text = "X";

            inputField.text = "";
            inputField.ActivateInputField();
        }
    }

    protected override void OnEnd()
    {
        if (inputField != null)
            inputField.DeactivateInputField();

#if UNITY_EDITOR
        Debug.Log("단어 맞추기 미니게임 종료");
#endif
    }
}