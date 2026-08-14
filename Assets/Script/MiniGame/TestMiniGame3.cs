using UnityEngine;
using UnityEngine.InputSystem;

public class TestMiniGame3 : MiniGameBase
{
    protected override void OnStart()
    {
        Debug.Log("마우스 왼쪽 클릭 미니게임 시작");
    }

private void Update()
{
    if (!IsPlaying)
        return;

    var mouse = Mouse.current;
    if (mouse == null)
        return;

    if (mouse.leftButton.wasPressedThisFrame)
    {
        Success();
    }
    else if (mouse.rightButton.wasPressedThisFrame)
    {
        Fail();
    }
}

    protected override void OnEnd()
    {
        Debug.Log("마우스 왼쪽 클릭 미니게임 종료");
    }
}