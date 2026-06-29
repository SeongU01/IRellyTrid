using UnityEngine;
using UnityEngine.InputSystem;

public class TestMiniGame2 : MiniGameBase
{
    protected override void OnStart()
    {
        Debug.Log("위 방향키 누르기 미니게임 시작");
    }

private void Update()
{
    if (!IsPlaying)
        return;

    var keyboard = Keyboard.current;
    if (keyboard == null)
        return;

    if (keyboard.anyKey.wasPressedThisFrame)
    {
        if (keyboard.upArrowKey.wasPressedThisFrame)
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
        Debug.Log("위 방향키 누르기 미니게임 종료");
    }
}