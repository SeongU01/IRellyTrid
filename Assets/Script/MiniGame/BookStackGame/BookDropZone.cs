using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BookDropZone : MonoBehaviour
{
    private static Sprite generatedZoneSprite;

    [Header("Zone")]
    [SerializeField] private Collider2D zoneCollider;
    [SerializeField] private SpriteRenderer zoneVisual;

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

        EnsureZoneVisual();
    }

    private void EnsureZoneVisual()
    {
        if (zoneVisual == null)
        {
            SpriteRenderer[] renderers =
                GetComponentsInChildren<SpriteRenderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].transform != transform)
                {
                    zoneVisual = renderers[i];
                    break;
                }
            }
        }

        if (zoneVisual == null || zoneVisual.sprite != null)
        {
            return;
        }

        if (generatedZoneSprite == null)
        {
            generatedZoneSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            generatedZoneSprite.name = "GeneratedDropZoneSquare";
        }

        zoneVisual.sprite = generatedZoneSprite;
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
