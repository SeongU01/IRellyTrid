using System.Collections;
using UnityEngine;

public class MiniGameManager : MonoBehaviour
{
    [Header("MiniGame Settings")]
    [SerializeField] private Transform miniGameRoot;
    // mini game프리팹을 담을 배열
    [SerializeField] private MiniGameData[] miniGames;

    [Header("Flow Settings")]
    [SerializeField] private float readyTime = 1f;
    [SerializeField] private float resultTime = 1f;

    private MiniGameBase currentMiniGame;
    private Coroutine gameRoutine;

    // 재생중인 게임의 인덱스를 저장하는 변수
    private int currentIndex = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        StartMiniGameFlow();
    }

public void StartMiniGameFlow()
{
    if (gameRoutine != null)
    {
        StopCoroutine(gameRoutine);
        gameRoutine = null;
    }

    DestroyCurrentMiniGame();

    gameRoutine = StartCoroutine(GameFlowRoutine());
}

    private IEnumerator GameFlowRoutine()
    {
        while (true)
        {
            MiniGameData data = GetRandomMiniGame();

            if (data == null)
            {
#if UNITY_EDITOR
                Debug.LogError("미니게임 데이터를 가져오지 못했습니다.");
#endif
                yield break;
            }
#if UNITY_EDITOR
            Debug.Log($"명령어 : {data.commandText}");
#endif

            yield return new WaitForSeconds(readyTime);

            if (!SpawnMiniGame(data))
            {
                yield return new WaitForSeconds(resultTime);
                continue;
            }
            float timer = data.timeLimit;
            // 미니게임이 재생중이고 제한시간이 남아있으면 계속 반복
            while (timer > 0f && currentMiniGame != null && currentMiniGame.IsPlaying)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            if (currentMiniGame != null && currentMiniGame.IsPlaying)
            {
#if UNITY_EDITOR
                Debug.Log("시간 초과");
#endif
                currentMiniGame.Timeout();
            }
            yield return new WaitForSeconds(resultTime);
        }

    }

    // random으로 미니게임을 선택하는 함수
    private MiniGameData GetRandomMiniGame()
    {
        if (miniGames == null || miniGames.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogError("등록된 미니게임이 없습니다.");
#endif
            return null;
        }

        int index = 0;
        int length = miniGames.Length;
        do
        {
            index = Random.Range(0, length);
        }
        while (length > 1 && index == currentIndex);

        currentIndex = index;
        return miniGames[index];
    }

    // 미니게임을 생성하는 함수
    private bool SpawnMiniGame(MiniGameData data)
    {
        if (data.prefab == null)
        {
#if UNITY_EDITOR
            Debug.LogError("미니게임 프리팹이 없습니다.");
#endif
            return false;
        }

        currentMiniGame = Instantiate(data.prefab, miniGameRoot);
        currentMiniGame.OnFinished += HandleMiniGameFinished;

        currentMiniGame.Init();
        currentMiniGame.Play();

        return true;
    }

    private void HandleMiniGameFinished(MiniGameResult result)
    {
#if UNITY_EDITOR
        Debug.Log(result.Success ? "성공" : "실패");
#endif
        DestroyCurrentMiniGame();
    }

    private void DestroyCurrentMiniGame()
    {
        if (currentMiniGame == null)
            return;
       
        currentMiniGame.OnFinished -= HandleMiniGameFinished;
        currentMiniGame.Stop();
        Destroy(currentMiniGame.gameObject);
        currentMiniGame = null;
    }
}
