using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class MemoryDefenseMiniGame : MiniGameBase
{
    [Serializable]
    private struct ObjectVisual
    {
        public Sprite sprite;
        public Color color;
    }

    [Header("Arena")]
    [SerializeField] private Sprite arenaSprite;
    [SerializeField] private Vector2 gameAreaCenter =
        new Vector2(-1.552f, -1.412f);
    [SerializeField, Min(0.5f)] private float arenaRadius = 3f;
    [SerializeField] private Color arenaColor = Color.black;
    [SerializeField, Range(32, 256)] private int circleResolution = 128;
    [SerializeField] private Vector2 arenaSpriteCircleCenterPixels =
        new Vector2(49.093f, 80.633f);
    [SerializeField, Min(1f)] private float arenaSpriteCircleRadiusPixels =
        48.102f;
    [SerializeField] private bool showArenaBoundary;
    [SerializeField] private Color arenaBoundaryColor = Color.red;
    [SerializeField, Min(0.01f)] private float arenaBoundaryWidth = 0.06f;

    [Header("Escaping Objects")]
    [SerializeField, Min(1)] private int objectCount = 5;
    [SerializeField, Min(0.1f)] private float objectSize = 0.55f;
    [SerializeField, Min(0.01f)] private float moveSpeed = 0.65f;
    [SerializeField, Min(0f)] private float spawnRadius = 1.6f;
    [SerializeField] private ObjectVisual[] objectVisuals;

    [Header("Reward")]
    [SerializeField, Min(0)] private int studyAmountPerRemainingObject = 3;

    private readonly List<EscapingMemoryObject> objects = new();
    private Camera mainCamera;
    private EscapingMemoryObject draggedObject;
    private Sprite generatedCircleSprite;
    private Sprite generatedSquareSprite;
    private Texture2D generatedCircleTexture;
    private Texture2D generatedSquareTexture;
    private Material arenaBoundaryMaterial;
    private int remainingObjectCount;

    protected override void OnStart()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "[MemoryDefenseMiniGame] A Main Camera is required.");
            Fail();
            return;
        }

        BuildArena();
        SpawnObjects();

#if UNITY_EDITOR
        Debug.Log(
            $"[MemoryDefenseMiniGame] Started with " +
            $"{remainingObjectCount} objects. Keep them inside for " +
            $"{timeLimit} seconds.");
#endif
    }

    private void Update()
    {
        if (!IsPlaying)
            return;

        HandlePointerInput();

        for (int index = 0; index < objects.Count; index++)
        {
            objects[index]?.Tick(Time.deltaTime);

            if (!IsPlaying)
                return;
        }
    }

    public override void Timeout()
    {
        if (!IsPlaying)
            return;

        CompleteWithRemainingObjects();
    }

    protected override void Success()
    {
        if (!IsPlaying)
            return;

        CompleteWithRemainingObjects();
    }

    protected override void OnEnd()
    {
        draggedObject?.CancelDrag();
        draggedObject = null;
    }

    private void OnDestroy()
    {
        if (generatedCircleSprite != null)
            Destroy(generatedCircleSprite);

        if (generatedSquareSprite != null)
            Destroy(generatedSquareSprite);

        if (generatedCircleTexture != null)
            Destroy(generatedCircleTexture);

        if (generatedSquareTexture != null)
            Destroy(generatedSquareTexture);

        if (arenaBoundaryMaterial != null)
            Destroy(arenaBoundaryMaterial);
    }

    public void HandleObjectEscaped(EscapingMemoryObject escapedObject)
    {
        if (!IsPlaying || escapedObject == null)
            return;

        if (draggedObject == escapedObject)
            draggedObject = null;

        remainingObjectCount = Mathf.Max(0, remainingObjectCount - 1);

#if UNITY_EDITOR
        Debug.Log(
            $"[MemoryDefenseMiniGame] An object escaped. " +
            $"Remaining: {remainingObjectCount}/{objectCount}.");
#endif

        if (remainingObjectCount <= 0)
            Fail();
    }

    private void CompleteWithRemainingObjects()
    {
        if (remainingObjectCount <= 0)
        {
            Fail();
            return;
        }

        int studyReward =
            remainingObjectCount * studyAmountPerRemainingObject;

#if UNITY_EDITOR
        Debug.Log(
            $"[MemoryDefenseMiniGame] Completed with " +
            $"{remainingObjectCount}/{objectCount} objects. " +
            $"Study reward: {studyReward}.");
#endif

        SuccessWithStudyReward(studyReward);
    }

    private void BuildArena()
    {
        if (arenaSprite == null)
        {
            generatedCircleTexture = CreateCircleTexture(circleResolution);
            generatedCircleSprite = Sprite.Create(
                generatedCircleTexture,
                new Rect(
                    0f,
                    0f,
                    generatedCircleTexture.width,
                    generatedCircleTexture.height),
                new Vector2(0.5f, 0.5f),
                generatedCircleTexture.width);
        }

        GameObject arenaObject = new GameObject(
            "Arena",
            typeof(SpriteRenderer));
        arenaObject.transform.SetParent(transform, false);
        arenaObject.transform.localPosition = gameAreaCenter;

        SpriteRenderer arenaRenderer =
            arenaObject.GetComponent<SpriteRenderer>();
        arenaRenderer.sprite = arenaSprite != null
            ? arenaSprite
            : generatedCircleSprite;
        arenaRenderer.color = arenaSprite != null
            ? Color.white
            : arenaColor;
        arenaRenderer.sortingOrder = -100;

        ApplyArenaSpriteTransform(arenaObject.transform);

        if (showArenaBoundary)
            BuildArenaBoundary();
    }

    private void ApplyArenaSpriteTransform(Transform arenaTransform)
    {
        float arenaScale = arenaRadius * 2f;

        if (arenaSprite != null &&
            arenaSprite.pixelsPerUnit > 0f &&
            arenaSpriteCircleRadiusPixels > 0f)
        {
            arenaScale = arenaRadius * arenaSprite.pixelsPerUnit /
                arenaSpriteCircleRadiusPixels;
            Vector2 circleOffsetPixels =
                arenaSpriteCircleCenterPixels - arenaSprite.pivot;
            Vector2 circleOffset = circleOffsetPixels /
                arenaSprite.pixelsPerUnit * arenaScale;
            arenaTransform.localPosition = gameAreaCenter - circleOffset;
        }

        arenaTransform.localScale = Vector3.one * arenaScale;
    }

    private void BuildArenaBoundary()
    {
        GameObject boundaryObject = new GameObject(
            "ArenaBoundary",
            typeof(LineRenderer));
        boundaryObject.transform.SetParent(transform, false);
        boundaryObject.transform.localPosition = gameAreaCenter;

        LineRenderer boundary = boundaryObject.GetComponent<LineRenderer>();
        boundary.useWorldSpace = false;
        boundary.loop = true;
        boundary.positionCount = circleResolution;
        boundary.startWidth = arenaBoundaryWidth;
        boundary.endWidth = arenaBoundaryWidth;
        boundary.startColor = arenaBoundaryColor;
        boundary.endColor = arenaBoundaryColor;
        boundary.sortingOrder = -90;

        Shader spriteShader = Shader.Find("Sprites/Default");

        if (spriteShader != null)
        {
            arenaBoundaryMaterial = new Material(spriteShader)
            {
                name = "Memory Defense Arena Boundary Material"
            };
            boundary.sharedMaterial = arenaBoundaryMaterial;
        }

        for (int index = 0; index < circleResolution; index++)
        {
            float angle = index * Mathf.PI * 2f / circleResolution;
            boundary.SetPosition(
                index,
                new Vector3(
                    Mathf.Cos(angle) * arenaRadius,
                    Mathf.Sin(angle) * arenaRadius,
                    0f));
        }
    }

    private void SpawnObjects()
    {
        generatedSquareTexture = CreateSquareTexture();
        generatedSquareSprite = Sprite.Create(
            generatedSquareTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);

        objects.Clear();
        remainingObjectCount = objectCount;
        List<Vector2> usedPositions = new List<Vector2>(objectCount);
        Vector2 arenaWorldCenter = transform.TransformPoint(
            new Vector3(gameAreaCenter.x, gameAreaCenter.y, 0f));

        for (int index = 0; index < objectCount; index++)
        {
            Vector2 spawnOffset = FindSpawnPosition(usedPositions);
            usedPositions.Add(spawnOffset);

            GameObject objectGameObject = new GameObject(
                $"MemoryObject_{index + 1}",
                typeof(SpriteRenderer),
                typeof(EscapingMemoryObject));
            objectGameObject.transform.SetParent(transform, false);
            objectGameObject.transform.localPosition =
                gameAreaCenter + spawnOffset;

            SpriteRenderer objectRenderer =
                objectGameObject.GetComponent<SpriteRenderer>();
            ObjectVisual visual = GetObjectVisual(index);
            objectRenderer.sprite = visual.sprite != null
                ? visual.sprite
                : generatedSquareSprite;
            objectRenderer.color = visual.color;
            objectRenderer.sortingOrder = 10;
            ApplyObjectScale(objectGameObject.transform, objectRenderer.sprite);

            EscapingMemoryObject escapingObject =
                objectGameObject.GetComponent<EscapingMemoryObject>();
            escapingObject.Initialize(
                this,
                objectRenderer,
                arenaWorldCenter,
                arenaRadius,
                moveSpeed,
                UnityEngine.Random.insideUnitCircle);
            objects.Add(escapingObject);
        }
    }

    private void HandlePointerInput()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        Vector2 worldPosition = ScreenToWorld(mouse.position.ReadValue());

        if (mouse.leftButton.wasPressedThisFrame)
        {
            draggedObject = FindObjectAt(worldPosition);
            if (draggedObject != null)
                GameAudioManager.PlayMouseClick();
            draggedObject?.BeginDrag();
        }

        if (draggedObject != null && mouse.leftButton.isPressed)
            draggedObject.DragTo(worldPosition);

        if (draggedObject == null ||
            !mouse.leftButton.wasReleasedThisFrame)
        {
            return;
        }

        draggedObject.DragTo(worldPosition);
        draggedObject.EndDrag();
        draggedObject = null;
    }

    private EscapingMemoryObject FindObjectAt(Vector2 worldPosition)
    {
        EscapingMemoryObject closest = null;
        float closestDistance = float.MaxValue;

        for (int index = 0; index < objects.Count; index++)
        {
            EscapingMemoryObject candidate = objects[index];

            if (candidate == null || !candidate.Contains(worldPosition))
                continue;

            float distance = Vector2.SqrMagnitude(
                (Vector2)candidate.transform.position - worldPosition);

            if (distance >= closestDistance)
                continue;

            closest = candidate;
            closestDistance = distance;
        }

        return closest;
    }

    private Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        float distanceFromCamera = Mathf.Abs(
            transform.position.z - mainCamera.transform.position.z);
        Vector3 screenPoint = new Vector3(
            screenPosition.x,
            screenPosition.y,
            distanceFromCamera);
        return mainCamera.ScreenToWorldPoint(screenPoint);
    }

    private Vector2 FindSpawnPosition(IReadOnlyList<Vector2> usedPositions)
    {
        float safeSpawnRadius = Mathf.Min(
            spawnRadius,
            Mathf.Max(0f, arenaRadius - objectSize));
        float minimumSeparation = objectSize * 1.25f;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector2 candidate =
                UnityEngine.Random.insideUnitCircle * safeSpawnRadius;
            bool overlaps = false;

            for (int index = 0; index < usedPositions.Count; index++)
            {
                if (Vector2.Distance(candidate, usedPositions[index]) >=
                    minimumSeparation)
                {
                    continue;
                }

                overlaps = true;
                break;
            }

            if (!overlaps)
                return candidate;
        }

        float angle = usedPositions.Count * Mathf.PI * 2f /
            Mathf.Max(1, objectCount);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
            safeSpawnRadius;
    }

    private ObjectVisual GetObjectVisual(int index)
    {
        if (objectVisuals != null && index < objectVisuals.Length)
            return objectVisuals[index];

        return new ObjectVisual
        {
            sprite = null,
            color = Color.HSVToRGB(
                index / (float)Mathf.Max(1, objectCount),
                0.75f,
                1f)
        };
    }

    private void ApplyObjectScale(Transform target, Sprite sprite)
    {
        if (sprite == null)
        {
            target.localScale = Vector3.one * objectSize;
            return;
        }

        Vector2 spriteSize = sprite.bounds.size;
        float largestDimension = Mathf.Max(spriteSize.x, spriteSize.y);
        float scale = largestDimension > 0f
            ? objectSize / largestDimension
            : objectSize;
        target.localScale = Vector3.one * scale;
    }

    private static Texture2D CreateSquareTexture()
    {
        Texture2D texture = new Texture2D(
            1,
            1,
            TextureFormat.RGBA32,
            false);
        texture.name = "Temporary Memory Object Square";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateCircleTexture(int resolution)
    {
        Texture2D texture = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false);
        texture.name = "Memory Defense Arena Circle";
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];
        Vector2 center = new Vector2(
            (resolution - 1) * 0.5f,
            (resolution - 1) * 0.5f);
        float radius = resolution * 0.5f;
        float radiusSquared = radius * radius;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 offset = new Vector2(x, y) - center;
                pixels[y * resolution + x] =
                    offset.sqrMagnitude <= radiusSquared
                        ? Color.white
                        : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private void OnValidate()
    {
        arenaRadius = Mathf.Max(0.5f, arenaRadius);
        objectCount = Mathf.Max(1, objectCount);
        objectSize = Mathf.Max(0.1f, objectSize);
        moveSpeed = Mathf.Max(0.01f, moveSpeed);
        spawnRadius = Mathf.Clamp(spawnRadius, 0f, arenaRadius);
        circleResolution = Mathf.Clamp(circleResolution, 32, 256);
        arenaSpriteCircleRadiusPixels = Mathf.Max(
            1f,
            arenaSpriteCircleRadiusPixels);
        arenaBoundaryWidth = Mathf.Max(0.01f, arenaBoundaryWidth);
        studyAmountPerRemainingObject = Mathf.Max(
            0,
            studyAmountPerRemainingObject);
    }
}
