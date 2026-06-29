using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// MiniGameBase 사용 예시용 테스트 미니게임.
/// Space 키를 누르면 성공, 다른 키를 누르면 실패한다.
/// 
/// 이 클래스를 참고해 MiniGameBase를 상속받은 미니게임을 작성하면 된다.
/// </summary>
public class TestMiniGame : MiniGameBase
{
    private int inputCount;

    /// <summary>
    /// 미니게임 생성 후 실행 직전에 초기화할 때 호출된다.
    /// 점수, 입력 횟수, 상태값 등을 초기화할 때 사용한다.
    /// </summary>
    public override void Init()
    {
        base.Init();

        inputCount = 0;

        Debug.Log("TestMiniGame 초기화");
    }

    /// <summary>
    /// 미니게임이 실제로 시작될 때 호출된다.
    /// 오브젝트 활성화, 타이머 시작, 안내 문구 출력 등을 여기서 처리한다.
    /// </summary>
    protected override void OnStart()
    {
        Debug.Log("스페이스바 누르기 미니게임 시작");
    }
    /// <summary>
    /// 미니 게임 로직을 처리하는 Update 함수.
    /// </summary>
private void Update()
{
    if (!IsPlaying)
        return;

    var keyboard = Keyboard.current;
    if (keyboard == null)
        return;

    if (keyboard.anyKey.wasPressedThisFrame)
    {
        inputCount++;

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Success();
        }
        else
        {
            Fail();
        }
    }
}

    /// <summary>
    /// 성공 처리 함수.
    /// 성공 이펙트, 점수 증가, 사운드 재생 등을 추가할 수 있다.
    /// 
    /// base.Success()를 호출해야 MiniGameBase의 Finish(true)가 실행되고,
    /// MiniGameManager로 성공 결과가 전달된다.
    /// </summary>
    protected override void Success()
    {
        base.Success();
    }

    /// <summary>
    /// 실패 처리 함수.
    /// 실패 이펙트, 피로도 증가, 사운드 재생 등을 추가할 수 있다.
    /// 
    /// base.Fail()을 호출해야 MiniGameBase의 Finish(false)가 실행되고,
    /// MiniGameManager로 실패 결과가 전달된다.
    /// </summary>
    protected override void Fail()
    {
        base.Fail();
    }

    /// <summary>
    /// 미니게임이 종료될 때 호출된다.
    /// 성공, 실패, 시간초과, Stop() 등으로 종료될 때 정리 작업을 수행한다.
    /// 생성한 오브젝트 제거, 코루틴 종료, 입력 상태 초기화 등을 여기서 처리한다.
    /// </summary>
    protected override void OnEnd()
    {
        Debug.Log("스페이스바 누르기 미니게임 종료");
    }
}