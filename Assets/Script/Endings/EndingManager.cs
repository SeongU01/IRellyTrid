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
    [Header("단일 UI 엔딩 설정")]
    public GameObject endingUIPanel; // 단일 연출용 UI 패널

    [Header("다중 씬 엔딩 설정")]
    public SceneSequence[] endingScenes; // 다중 씬 연출용 배열

    // 파괴 방지 적용을 위한 초기화 작업
    private void Awake()
    {
        // 최상위 객체로의 위치 변경 (DontDestroyOnLoad 조건 충족)
        transform.SetParent(null);
        
        // 씬 전환 시 객체 파괴 방지
        DontDestroyOnLoad(gameObject);
    }

    // 시작 시 초기 UI 비활성화
    private void Start()
    {
        if (endingUIPanel != null)
        {
            endingUIPanel.SetActive(false);
        }
    }

    // 씬 로드 이벤트 구독 등록
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 씬 로드 이벤트 구독 해제
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬 로드 시 실행되는 콜백 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 로드된 씬의 이름 확인 및 엔딩 매니저 객체 파괴
        // "Title" 문자열은 실제 타이틀 씬의 이름으로 변경 필요
        if (scene.name == "Title") 
        {
            Destroy(gameObject);
        }
    }

    // 외부(BlinkEffect 등)에서 호출할 수 있는 엔딩 흐름 시작 함수
    public void StartEndingFlow()
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