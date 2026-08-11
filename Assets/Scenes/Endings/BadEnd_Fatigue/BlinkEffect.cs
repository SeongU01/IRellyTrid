using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;

public class BlinkEffect : MonoBehaviour
{
    public Image fadeImage;
    public float blinkSpeed = 0.1f;
    public float finalFadeSpeed = 0.5f;

    // 깜빡임 연출 시작 및 콜백 등록
    public void StartBlinkEffect(Action onComplete)
    {
        StartCoroutine(BlinkSequence(onComplete));
    }

    // 전체 깜빡임 연출 코루틴
    private IEnumerator BlinkSequence(Action onComplete)
    {
        // 첫 번째 깜빡임
        yield return StartCoroutine(FadeAlpha(0f, 1f, blinkSpeed));
        yield return StartCoroutine(FadeAlpha(1f, 0f, blinkSpeed));

        // 두 번째 깜빡임
        yield return StartCoroutine(FadeAlpha(0f, 1f, blinkSpeed));
        yield return StartCoroutine(FadeAlpha(1f, 0f, blinkSpeed));

        // 마지막 완전 암전 적용
        yield return StartCoroutine(FadeAlpha(0f, 1f, finalFadeSpeed));

        // 암전 완료 후 콜백 실행
        onComplete?.Invoke();
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