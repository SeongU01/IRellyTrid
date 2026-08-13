using System.Collections;
using UnityEngine;

// 페이드 효과 전담 유틸리티 클래스
public class FadeUtility : MonoBehaviour
{
    [Header("페이드 대상 설정")]
    public CanvasGroup targetCanvasGroup; // 대상 캔버스 그룹 컴포넌트

    // 페이드 인 실행 진입점
    public Coroutine DoFadeIn(float duration)
    {
        // 기존 알파 값을 0에서 1로 변경하는 코루틴 반환
        return StartCoroutine(FadeCoroutine(0f, 1f, duration));
    }

    // 페이드 아웃 실행 진입점
    public Coroutine DoFadeOut(float duration)
    {
        // 기존 알파 값을 1에서 0으로 변경하는 코루틴 반환
        return StartCoroutine(FadeCoroutine(1f, 0f, duration));
    }

    // 실제 투명도 조절 코루틴
    private IEnumerator FadeCoroutine(float startAlpha, float targetAlpha, float duration)
    {
        // 경과 시간 누적 변수
        float elapsedTime = 0f;

        // 시작 투명도 강제 적용
        targetCanvasGroup.alpha = startAlpha;

        // 경과 시간이 목표 시간에 도달할 때까지 반복
        while (elapsedTime < duration)
        {
            // 타임 스케일 무시 델타 타임 누적
            elapsedTime += Time.unscaledDeltaTime;

            // 진행률 계산 (0에서 1 사이의 비율)
            float t = elapsedTime / duration;

            // 현재 진행률에 따른 알파 값 선형 보간
            targetCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            // 다음 프레임 대기
            yield return null;
        }

        // 오차 보정을 위한 최종 목표 투명도 고정
        targetCanvasGroup.alpha = targetAlpha;
    }
}