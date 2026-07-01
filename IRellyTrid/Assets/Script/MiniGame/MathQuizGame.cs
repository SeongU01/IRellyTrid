using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;

public class MathQuizGame : MiniGameBase
{
    [Header("UI")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text inputText;
    // TODO : 결과 표시용 텍스트 나중에 제거하고 연출로 바꾸면 됨
    [SerializeField] private TMP_Text resultText;
    // TODO : 남은 시간 표기용, 나중에 다른걸로 교체
    [SerializeField] private TMP_Text timeText;

    [Header("Quiz Settings")]
    [SerializeField] private int totalQuestionCount = 20;
    [SerializeField] private int minNumber = 1;
    [SerializeField] private int maxNumber = 9;

    private int currentQuestionIndex;
    private int correctAnswer;
    private string currentInput = "";
    private float remainTime;
    protected override void OnStart()
    {
        currentQuestionIndex = 0;
        currentInput = "";
        remainTime = timeLimit;
        if(resultText != null)
        {
            resultText.text = "";
        }

        GenerateQuestion();
        UpdateTimerText();
#if UNITY_EDITOR
        Debug.Log("간단 연산 게임 시작");
#endif
    }
    private void UpdateTimerText()
    {
        if (timeText != null)
        {
            timeText.text = Mathf.CeilToInt(remainTime).ToString();
        }
    }
    private void Update()
    {
        if(!IsPlaying)
        {
            return;
        }

        remainTime -= Time.deltaTime;
        UpdateTimerText();

        HandleNumberInput();
        HandleBackSpace();
        HandleSubmit();
    }

    private void HandleNumberInput()
    {
        Keyboard keyboard = Keyboard.current;
        if(keyboard== null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("키보드 입력을 감지할 수 없습니다.");
#endif
            return;
        }

        // 0~9까지의 숫자 키 입력 처리
        for (int i =0; i<=9; ++i)
        {
            Key key = Key.Digit0 + i;
            // 키보드의 숫자 키가 눌렸는지 확인
            if (keyboard[key].wasPressedThisFrame)
            {
                currentInput += i.ToString();
                UpdateInputText();
                return;
            }

            Key numpadKey = Key.Numpad0 + i;
            // 키보드의 숫자 패드 키가 눌렸는지 확인
            if (keyboard[numpadKey].wasPressedThisFrame)
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

        if (keyboard == null)
        {
            return;
        }
        if(!keyboard.backspaceKey.wasPressedThisFrame)
        {
            return;
        }
        // 지울 문자가 없으면 처리하지 않음
        if (currentInput.Length<=0)
        {
            return;
        }

        currentInput = currentInput.Substring(0, currentInput.Length - 1);
        UpdateInputText();
    }

    private void HandleSubmit()
    {
        Keyboard keyboard = Keyboard.current;
        
        if(keyboard == null)
        {
            return;
        }
        if(!keyboard.enterKey.wasPressedThisFrame && 
           !keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            return;
        }
        if(string.IsNullOrEmpty(currentInput))
        {
            return;
        }
        
        int playerAnswer = int.Parse(currentInput);

        if (playerAnswer == correctAnswer)
        {
            if (resultText != null)
            {
                resultText.text = "O";
            }
        }
        else
        {
            if(resultText != null)
            {
                resultText.text = "X";
            }
        }

        currentQuestionIndex++;
        

        if(currentQuestionIndex>= totalQuestionCount)
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
        int op = Random.Range(0, 4);
        int left = 0;
        int right = 0;
        char operatorChar = '+';

        switch (op)
        {
            // 덧셈
            case 0:
                left = Random.Range(minNumber, maxNumber + 1);
                right = Random.Range(minNumber, maxNumber + 1);

                correctAnswer = left + right;
                operatorChar = '+';
                break;

            // 뺄셈 (음수가 나오지 않도록)
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

            // 곱셈
            case 2:
                left = Random.Range(minNumber, maxNumber + 1);
                right = Random.Range(minNumber, maxNumber + 1);

                correctAnswer = left * right;
                operatorChar = '×';
                break;

            // 나눗셈 (항상 정수)
            case 3:
                right = Random.Range(minNumber, maxNumber + 1);

                correctAnswer = Random.Range(minNumber, maxNumber + 1);
                left = right * correctAnswer;

                operatorChar = '÷';
                break;
        }

        questionText.text = $"{left} {operatorChar} {right} = ?";

        progressText.text = $"{currentQuestionIndex + 1} / {totalQuestionCount}";

        UpdateInputText();

#if UNITY_EDITOR
        Debug.Log($"정답 : {correctAnswer}");
#endif
    }

    private void UpdateInputText()
    {
        if(inputText != null)
        {
            inputText.text = currentInput;
        }
    }

    protected override void OnEnd()
    {
#if UNITY_EDITOR
        Debug.Log("간단 연산 게임 종료");
#endif
    }

}
