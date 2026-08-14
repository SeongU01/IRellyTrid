using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

[DisallowMultipleComponent]
public sealed class MiniGameEventSystemBootstrap : MonoBehaviour
{
    private void Awake()
    {
        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        EventSystem existingEventSystem =
            FindAnyObjectByType<EventSystem>();

        if (existingEventSystem != null)
            return;

        new GameObject(
            nameof(EventSystem),
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));

#if UNITY_EDITOR
        Debug.Log(
            "[MiniGameEventSystemBootstrap] EventSystem with " +
            "InputSystemUIInputModule was created for the current scene.");
#endif
    }
}
