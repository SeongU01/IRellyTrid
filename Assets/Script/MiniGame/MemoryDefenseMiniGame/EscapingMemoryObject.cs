using UnityEngine;

[DisallowMultipleComponent]
public sealed class EscapingMemoryObject : MonoBehaviour
{
    private MemoryDefenseMiniGame owner;
    private SpriteRenderer spriteRenderer;
    private Vector2 direction;
    private Vector2 arenaCenter;
    private float arenaRadius;
    private float moveSpeed;
    private bool isDragging;
    private bool hasEscaped;

    public bool HasEscaped => hasEscaped;
    public bool IsDragging => isDragging;

    public void Initialize(
        MemoryDefenseMiniGame gameOwner,
        SpriteRenderer targetRenderer,
        Vector2 center,
        float radius,
        float speed,
        Vector2 initialDirection)
    {
        owner = gameOwner;
        spriteRenderer = targetRenderer;
        arenaCenter = center;
        arenaRadius = radius;
        moveSpeed = speed;
        direction = NormalizeOrRandom(initialDirection);
        isDragging = false;
        hasEscaped = false;
    }

    public void Tick(float deltaTime)
    {
        if (hasEscaped || isDragging)
            return;

        transform.position +=
            (Vector3)(direction * moveSpeed * deltaTime);

        if (Vector2.Distance(transform.position, arenaCenter) < arenaRadius)
            return;

        hasEscaped = true;
        gameObject.SetActive(false);
        owner?.HandleObjectEscaped(this);
    }

    public bool Contains(Vector2 worldPosition)
    {
        if (hasEscaped || spriteRenderer == null)
            return false;

        Vector3 point = new Vector3(
            worldPosition.x,
            worldPosition.y,
            spriteRenderer.bounds.center.z);
        return spriteRenderer.bounds.Contains(point);
    }

    public void BeginDrag()
    {
        if (!hasEscaped)
            isDragging = true;
    }

    public void DragTo(Vector2 worldPosition)
    {
        if (!isDragging || hasEscaped)
            return;

        transform.position = new Vector3(
            worldPosition.x,
            worldPosition.y,
            transform.position.z);
    }

    public void EndDrag()
    {
        if (!isDragging || hasEscaped)
            return;

        isDragging = false;
        Vector2 outwardDirection =
            (Vector2)transform.position - arenaCenter;
        direction = NormalizeOrRandom(outwardDirection);
    }

    public void CancelDrag()
    {
        isDragging = false;
    }

    private static Vector2 NormalizeOrRandom(Vector2 candidate)
    {
        if (candidate.sqrMagnitude > 0.0001f)
            return candidate.normalized;

        Vector2 randomDirection = Random.insideUnitCircle;

        if (randomDirection.sqrMagnitude <= 0.0001f)
            return Vector2.right;

        return randomDirection.normalized;
    }
}
