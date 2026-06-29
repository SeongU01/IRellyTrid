using UnityEngine;
using UnityEngine.InputSystem;
public class TestMiniGame : MiniGameBase
{
    protected override void OnStart()
    {
        Debug.Log("스페이스바 누르기 미니게임 시작");
    }
    private void Update()
    {
        if (!IsPlaying)
            return;

        if (Keyboard.current.anyKey.wasPressedThisFrame)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Success();
            }
            else
            {
                Fail();
            }
        }
    }
    protected override void Success()
    {
        base.Success();
    }
    protected override void Fail()
    {
        base.Fail();
    }
    protected override void OnEnd()
    {
        Debug.Log("스페이스 누르기 미니게임 종료");
    }
}
