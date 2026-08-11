using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SortingCategoryView : MonoBehaviour
{
    private Image iconImage;
    private TMP_Text labelText;
    public void Initialize(
        Image icon,
        TMP_Text label,
        SortCategoryData data,
        Sprite resolvedSprite)
    {
        iconImage = icon;
        labelText = label;

        iconImage.sprite = resolvedSprite;
        iconImage.color = resolvedSprite != null
            ? Color.white
            : data.fallbackColor;
        iconImage.preserveAspect = true;

        string categoryName = string.IsNullOrWhiteSpace(
            data.categoryName)
            ? "Category"
            : data.categoryName;

        labelText.text = categoryName;
    }
}
