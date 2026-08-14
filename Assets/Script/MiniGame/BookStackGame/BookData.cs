using UnityEngine;
using System;

public enum BookAssetVariant
{
    Base,
    First,
    Second
}

[Serializable]
public class BookData
{
    public string bookName;
    public Sprite sprite;
    public Color color = Color.white;
    public BookAssetVariant assetVariant;
}   
