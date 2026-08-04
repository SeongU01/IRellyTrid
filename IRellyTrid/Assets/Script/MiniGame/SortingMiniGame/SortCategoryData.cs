using UnityEngine;

public enum SortSide
{
    Left,
    Right
}

[System.Serializable]
public class SortCategoryData
{
    public string categoryName = "Category";
    public Sprite sprite;
    public SortSide side;
    public Color fallbackColor = Color.white;
}
