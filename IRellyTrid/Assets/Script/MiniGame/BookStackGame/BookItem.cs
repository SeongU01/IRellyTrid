using Unity.VisualScripting;
using UnityEngine;

public class BookItem : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private BookStackGame owner;
    private BookData data;
    private bool isTarget;
    private int stackIndex;

    public int StackIndex => stackIndex;
    public bool IsTarget => isTarget;
    public string BookName => data != null ? data.bookName : "";

    public void Init(BookStackGame owmer, BookData data, bool isTarget, int stackIndex)
    {
        this.owner = owmer;
        this.data = data;
        this.stackIndex = stackIndex;
        this.isTarget = isTarget;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = data.sprite;
            spriteRenderer.color = data.color;
        }
        gameObject.name = isTarget ?  $"TargetBook_{data.bookName}" : data.bookName;
    }

    private void OnMouseDown()
    {
        if (owner == null)
        {
            return;
        }

        owner.OnBookClicked(this);
    }
}
