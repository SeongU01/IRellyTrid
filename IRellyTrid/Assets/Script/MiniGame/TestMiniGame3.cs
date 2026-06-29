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

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Success();
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Fail();
        }
    }

    protected override void OnEnd()
    {
        Debug.Log("마우스 왼쪽 클릭 미니게임 종료");
    }
}