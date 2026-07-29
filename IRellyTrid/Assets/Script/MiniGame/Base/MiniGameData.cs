using UnityEngine;

[System.Serializable]
public class MiniGameData
{
    public string name;
    public string commandText;
    public float timeLimit = 30f;
    public bool isBonus;
    public MiniGameBase prefab;
}
