using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위한 네임스페이스 추가

// 페이드 연출 단계를 정의하는 구조체
[System.Serializable]
public struct FadeStep
{
    public FadeUtility targetFadeUtility; // 대상 페이드 유틸리티 참조
    public bool isFadeIn; // 페이드 인 적용 여부 (체크 시 페이드 인, 해제 시 페이드 아웃)
    public float fadeDuration; // 페이드 진행 소요 시간
    public float waitTimeAfter; // 페이드 완료 후 추가 대기 시간
}

// 엔딩 시퀀스 순차 제어 클래스
public class EndingSequenceController : MonoBehaviour
{
    [Header("시퀀스 순서 설정")]
    public List<FadeStep> fadeSteps; // 시퀀스 단계 리스트

    [Header("크레딧 씬 설정")]
    public string nextSceneName; // 연출 종료 후 호출할 씬 이름 (예: "EndingCredit")

    private void Awake()
    {
        HashSet<CanvasGroup> initializedGroups = new HashSet<CanvasGroup>();

        foreach (FadeStep step in fadeSteps)
        {
            CanvasGroup canvasGroup = step.targetFadeUtility != null
                ? step.targetFadeUtility.targetCanvasGroup
                : null;

            if (canvasGroup == null || !initializedGroups.Add(canvasGroup))
                continue;

            canvasGroup.alpha = step.isFadeIn ? 0f : 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    // 객체 활성화 시 자동 호출되는 콜백 함수
    private void OnEnable()
    {
        // 시퀀스 실행 코루틴 시작
        StartCoroutine(PlaySequence());
    }

    // 실제 연출 순서 제어 코루틴
    private IEnumerator PlaySequence()
    {
        // 첫 프레임 스파이크 현상 방지를 위한 1프레임 대기
        yield return null;

        // 리스트 내장 요소 순회 반복문
        foreach (FadeStep step in fadeSteps)
        {
            // 타겟 유틸리티 할당 확인
            if (step.targetFadeUtility != null)
            {
                // 페이드 인 아웃 분기 처리 및 완료 대기
                if (step.isFadeIn)
                {
                    yield return step.targetFadeUtility.DoFadeIn(step.fadeDuration);

                    CanvasGroup canvasGroup =
                        step.targetFadeUtility.targetCanvasGroup;
                    if (canvasGroup != null)
                    {
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                    }
                }
                else
                {
                    yield return step.targetFadeUtility.DoFadeOut(step.fadeDuration);
                }
            }

            // 추가 대기 시간 존재 여부 확인
            if (step.waitTimeAfter > 0f)
            {
                // 타임 스케일 무시 대기 적용
                yield return new WaitForSecondsRealtime(step.waitTimeAfter);
            }
        }

        // 시퀀스 연출이 모두 끝난 후의 추가 로직
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // 설정된 다음 씬 비동기 로드
            SceneManager.LoadSceneAsync(nextSceneName);
        }
    }
}
