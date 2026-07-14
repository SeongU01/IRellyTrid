using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BookDropZone : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private Collider2D zoneCollider;

    [Header("Book Placement")]
    [SerializeField] private Transform placedBookRoot;
    [SerializeField] private Vector2 placedStartOffset;
    [SerializeField] private float placedBookSpacing = 0.4f;

    public Transform PlacedBookRoot
    {
        get
        {
            if (placedBookRoot != null)
            {
                return placedBookRoot;
            }

            return transform;
        }
    }

    private void Awake()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider2D>();
        }
    }

    public bool Contains(Vector3 worldPosition)
    {
        if (zoneCollider == null)
        {
            return false;
        }

        return zoneCollider.OverlapPoint(worldPosition);
    }

    public Vector3 GetPlacedLocalPosition(int placedIndex)
    {
        int index = Mathf.Max(0, placedIndex);

        return new Vector3(
            placedStartOffset.x,
            placedStartOffset.y +
            index * placedBookSpacing,
            -index * 0.01f);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider2D>();
        }

        placedBookSpacing =
            Mathf.Max(0f, placedBookSpacing);
    }
#endif
}
