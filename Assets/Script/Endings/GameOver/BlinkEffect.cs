using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class BlinkEffect : MonoBehaviour
{
    public Image fadeImage;
    public int blinkCount = 2; // 깜빡임 반복 횟수
    public float blinkSpeed = 0.1f;
    public float finalFadeSpeed = 0.5f;

    // 깜빡임 연출 완료 시 실행할 외부 콜백 이벤트
    public UnityEvent onBlinkCompleted;

    // 컴포넌트 활성화 시 자동으로 연출 시작
    private void Start()
    {
        StartCoroutine(BlinkSequence());
    }

    // 전체 깜빡임 연출 코루틴
    private IEnumerator BlinkSequence()
    {
        // 설정된 횟수만큼 깜빡임 반복
        for (int i = 0; i < blinkCount; i++)
        {
            yield return StartCoroutine(FadeAlpha(0f, 1f, blinkSpeed));
            yield return StartCoroutine(FadeAlpha(1f, 0f, blinkSpeed));
        }

        // 마지막 완전 암전 적용
        yield return StartCoroutine(FadeAlpha(0f, 1f, finalFadeSpeed));

        // 암전 완료 후 유니티 이벤트 호출
        onBlinkCompleted?.Invoke();
    }

    // 투명도 수치 조절 코루틴
    private IEnumerator FadeAlpha(float startAlpha, float targetAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color color = fadeImage.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            fadeImage.color = color;
            yield return null;
        }

        // 최종 투명도 고정
        color.a = targetAlpha;
        fadeImage.color = color;
    }
}