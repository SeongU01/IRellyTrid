using UnityEngine;

[System.Serializable]
public class MiniGameData
{
    public string name;
    public string commandText;
    public float timeLimit = 30f;
    public MiniGameBase prefab;
}
