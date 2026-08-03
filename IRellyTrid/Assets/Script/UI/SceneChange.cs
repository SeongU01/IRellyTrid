using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SceneChange : MonoBehaviour
{
    // Set the target scene name in the Inspector and assign `LoadSceneByName` to the Button's OnClick.
    public string sceneName;

    // Or set a scene index and assign `LoadSceneByIndex` to the Button's OnClick.
    public int sceneIndex = -1;

    // Call this from a UI Button (no parameter) to load the `sceneName` set on this component.
    public void LoadSceneByName()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"Scene name is empty on GameObject: {gameObject.name}");
            return;
        }

        Debug.Log($"SceneChange: Loading scene '{sceneName}' from GameObject '{gameObject.name}'");

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"SceneChange: Scene '{sceneName}' cannot be loaded. Check Build Settings and spelling.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // Call this from a UI Button to load a scene by index.
    public void LoadSceneByIndex()
    {
        if (sceneIndex >= 0)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.LogWarning("Scene index is not set on GameObject: " + gameObject.name);
        }
    }

}
