using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingManager : MonoBehaviour
{
    public BlinkEffect blinkEffect;
    public GameObject endingUIPanel; 

    // 시작 시 연출 지시
    private void Start()
    {
        // 초기 UI 비활성화
        endingUIPanel.SetActive(false);
        
        // 깜빡임 연출 실행 및 완료 시점의 콜백 함수 전달
        blinkEffect.StartBlinkEffect(OnBlinkCompleted);
    }

    // 깜빡임 완료 후 실행
    private void OnBlinkCompleted()
    {
        StartCoroutine(HandleSceneTransition());
    }

    // 기존 씬 언로드 및 UI 출력 코루틴
    private IEnumerator HandleSceneTransition()
    {
        // 현재 씬 뒤에 있는 기존 씬 언로드 (인덱스 0번)
        if (SceneManager.sceneCount > 1)
        {
            Scene previousScene = SceneManager.GetSceneAt(0);
            yield return SceneManager.UnloadSceneAsync(previousScene);
        }

        // 마스터가 인스펙터에 넣은 엔딩 UI 활성화
        endingUIPanel.SetActive(true);
    }
}