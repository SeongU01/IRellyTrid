using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArrowPathCellView : MonoBehaviour
{
    private Image backgroundImage;
    private Image arrowImage;
    private TMP_Text markerText;

    private Color normalColor;
    private Color activeColor;
    private Color completedColor;
    private Color wrongColor;
    private Color goalColor;

    public void Initialize(
        Image background,
        Image arrow,
        TMP_Text marker,
        Color normal,
        Color active,
        Color completed,
        Color wrong,
        Color goal)
    {
        backgroundImage = background;
        arrowImage = arrow;
        markerText = marker;

        normalColor = normal;
        activeColor = active;
        completedColor = completed;
        wrongColor = wrong;
        goalColor = goal;
    }

    public void ShowArrow(Sprite sprite, bool isStart)
    {
        arrowImage.sprite = sprite;
        arrowImage.enabled = sprite != null;
        arrowImage.color = Color.white;

        markerText.text = isStart ? "START" : "";
        markerText.enabled = isStart;
        markerText.rectTransform.anchoredPosition =
            new Vector2(0f, 48f);

        backgroundImage.color = normalColor;
    }

    public void ShowGoal()
    {
        arrowImage.sprite = null;
        arrowImage.enabled = false;

        markerText.text = "GOAL";
        markerText.enabled = true;
        markerText.rectTransform.anchoredPosition = Vector2.zero;

        backgroundImage.color = goalColor;
    }

    public void SetActiveState()
    {
        backgroundImage.color = activeColor;
    }

    public void SetCompleted()
    {
        backgroundImage.color = completedColor;
        arrowImage.color = new Color(1f, 1f, 1f, 0.55f);
    }

    public void SetWrong()
    {
        backgroundImage.color = wrongColor;
    }

    public void SetGoalReached()
    {
        backgroundImage.color = completedColor;
    }
}
