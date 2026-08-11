using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 이름과 유지 시간을 묶은 구조체
[System.Serializable]
public struct SceneSequence
{
    public string sceneName; // 씬 이름
    public float duration; // 유지 시간
}

public class EndingManager : MonoBehaviour
{
    [Header("시각 효과 설정")]
    public BlinkEffect blinkEffect;
    public bool useBlinkEffect = true; // 깜빡임 효과 사용 여부

    [Header("단일 UI 엔딩 설정")]
    public GameObject endingUIPanel; // 단일 연출용 UI 패널

    [Header("다중 씬 엔딩 설정")]
    public SceneSequence[] endingScenes; // 다중 씬 연출용 배열

    // 시작 시 연출 흐름 지시
    private void Start()
    {
        // 초기 UI 비활성화
        if (endingUIPanel != null)
        {
            endingUIPanel.SetActive(false);
        }
        
        // 깜빡임 사용 여부에 따른 분기 처리
        if (useBlinkEffect && blinkEffect != null)
        {
            // 깜빡임 실행 및 완료 시 콜백 연결
            blinkEffect.StartBlinkEffect(OnBlinkCompleted);
        }
        else
        {
            // 깜빡임 생략 시 즉시 다음 단계 실행
            OnBlinkCompleted();
        }
    }

    // 초기 연출 완료 후 실행
    private void OnBlinkCompleted()
    {
        StartCoroutine(HandleEndingFlow());
    }

    // 조건에 따른 엔딩 흐름 제어 코루틴
    private IEnumerator HandleEndingFlow()
    {
        // 기존 게임 씬 언로드 (인덱스 0번)
        if (SceneManager.sceneCount > 1)
        {
            Scene previousScene = SceneManager.GetSceneAt(0);
            yield return SceneManager.UnloadSceneAsync(previousScene);
        }

        // 다중 씬 배열의 존재 및 할당 여부 확인
        bool isMultiScene = endingScenes != null && endingScenes.Length > 0;

        if (isMultiScene)
        {
            // 화면을 덮은 깜빡임 객체 비활성화 (씬 렌더링을 위함)
            if (blinkEffect != null)
            {
                blinkEffect.gameObject.SetActive(false);
            }

            // 배열을 순회하며 다중 씬 재생
            for (int i = 0; i < endingScenes.Length; i++)
            {
                string currentScene = endingScenes[i].sceneName;
                float waitTime = endingScenes[i].duration;

                // 씬 로드 (Additive 방식)
                yield return SceneManager.LoadSceneAsync(currentScene, LoadSceneMode.Additive);

                // 설정된 시간만큼 대기
                yield return new WaitForSeconds(waitTime);

                // 마지막 씬이 아닐 경우 언로드 진행
                if (i < endingScenes.Length - 1)
                {
                    yield return SceneManager.UnloadSceneAsync(currentScene);
                }
            }
        }
        else
        {
            // 단일 UI 출력 연출 진행
            if (endingUIPanel != null)
            {
                endingUIPanel.SetActive(true);
            }
        }
    }
}