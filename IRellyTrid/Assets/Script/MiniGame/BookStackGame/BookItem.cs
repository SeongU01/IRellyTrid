using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class BookItem : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private BookStackGame owner;
    private BookData data;
    private Camera mainCamera;

    private Vector3 dragOffset;
    private bool isDragging;
    private bool isLocked;
    private bool isTarget;

    public bool IsTarget => isTarget;
    public bool IsLocked => isLocked;
    public string BookName => data != null ? data.bookName : "";

    public void Init(
        BookStackGame owner,
        BookData data,
        bool isTarget,
        int sortingOrder)
    {
        this.owner = owner;
        this.data = data;
        this.isTarget = isTarget;

        isDragging = false;
        isLocked = false;

        mainCamera = Camera.main;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null && data != null)
        {
            spriteRenderer.sprite = data.sprite;
            spriteRenderer.color = data.color;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        if (data != null)
        {
            gameObject.name = isTarget
                ? $"TargetBook_{data.bookName}"
                : data.bookName;
        }
    }

    private void OnMouseDown()
    {
        if (isLocked || owner == null || mainCamera == null)
        {
            return;
        }

        owner.BeginBookDrag(this);

        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        dragOffset = transform.position - mouseWorldPosition;

        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (!isDragging || isLocked)
        {
            return;
        }

        Vector3 targetPosition = GetMouseWorldPosition() + dragOffset;
        targetPosition.z = transform.position.z;

        transform.position = targetPosition;
    }

    private void OnMouseUp()
    {
        if (!isDragging || isLocked)
        {
            return;
        }

        isDragging = false;
        owner.OnBookDropped(this);
    }

    public void BringToFront(int sortingOrder, float zPosition)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }

        Vector3 position = transform.position;
        position.z = zPosition;
        transform.position = position;
    }

    public void LockAt(
        Transform parent,
        Vector3 localPosition,
        int sortingOrder)
    {
        isDragging = false;
        isLocked = true;

        transform.SetParent(parent);
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.identity;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || mainCamera == null)
        {
            return transform.position;
        }

        Vector2 screenPosition = mouse.position.ReadValue();
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, 0f));

        worldPosition.z = transform.position.z;
        return worldPosition;
    }
}