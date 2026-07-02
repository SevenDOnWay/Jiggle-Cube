using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class JellyCubeFactory : MonoBehaviour {

    [SerializeField] CubeColorSO colorTemplate;
    [SerializeField] GameObject jellyPrefab;

    float unitSize = 1f;
    float jellySizeRatio = 0.82f;
    float jellyGapRatio = 0.08f;
    int defaultSlotResolution = 2;

    PlayScreen playScreen;

    [Inject]
    public void Construct(PlayScreen playScreen) {
        this.playScreen = playScreen;
        ConfigureFromPlayScreen();
    }

    void ConfigureFromPlayScreen() {
        if (playScreen == null) {
            return;
        }

        unitSize = playScreen.CellVisualSize > 0f ? playScreen.CellVisualSize : 1f;
        jellySizeRatio = playScreen.JellySizeRatio;
        jellyGapRatio = playScreen.JellyGapRatio;
        defaultSlotResolution = playScreen.CellSlotResolution;
    }

    public JellyCube CreateJelly(Vector3 worldPosition, Transform parent = null) {
        return CreateJelly(Color.white, worldPosition, parent);
    }

    public JellyCube CreateJelly(Color color, Vector3 worldPosition, Transform parent = null) {
        if (jellyPrefab == null) {
            Debug.LogWarning("JellyCubeFactory needs a jellyPrefab before it can create jelly cubes.", this);
            return null;
        }

        GameObject jellyObject = Instantiate(jellyPrefab, worldPosition, jellyPrefab.transform.rotation, parent);
        DisableJellyController(jellyObject);
        JellyCube jelly = jellyObject.GetComponent<JellyCube>();

        if (jelly == null) {
            jelly = jellyObject.AddComponent<JellyCube>();
        }

        jelly.color = color;
        jelly.occupieSlot = new List<Vector2Int> { Vector2Int.zero };
        ApplyColor(jelly, color);
        ResizeJelly(jelly, 1);
        return jelly;
    }

    void DisableJellyController(GameObject jellyObject) {
        Controller controller = jellyObject != null ? jellyObject.GetComponent<Controller>() : null;

        if (controller != null) {
            controller.enabled = false;
        }
    }

    public List<JellyCube> CreateJellyShape(JellyShapeSO shape, Vector3 cellCenter, Transform parent = null) {
        List<JellyCube> jellies = new List<JellyCube>();

        if (jellyPrefab == null) {
            Debug.LogWarning("JellyCubeFactory needs a jellyPrefab before it can create jelly shapes.", this);
            return jellies;
        }

        int resolution = shape != null ? Mathf.Max(1, shape.slotResolution) : defaultSlotResolution;
        IReadOnlyList<JellyPartShapeData> parts = GetShapeParts(shape);
        Transform root = parent != null ? parent : transform;

        for (int i = 0; i < parts.Count; i++) {
            JellyPartShapeData part = parts[i];

            if (part == null || part.occupiedSlots == null || part.occupiedSlots.Count == 0) {
                continue;
            }

            JellyCube jelly = CreateJellyPart(part.occupiedSlots, GetRandomColor(), cellCenter, root, resolution);

            if (jelly != null) {
                jellies.Add(jelly);
            }
        }

        return jellies;
    }

    public List<JellyCube> CreateJellies(IReadOnlyList<Vector2Int> occupiedSlots, Color color, Vector3 cellCenter, Transform parent = null, int slotResolution = 0) {
        List<JellyCube> jellies = new List<JellyCube>();

        if (occupiedSlots == null || occupiedSlots.Count == 0) {
            JellyCube jelly = CreateJelly(color, cellCenter, parent);

            if (jelly != null) {
                jellies.Add(jelly);
            }

            return jellies;
        }

        int resolution = ResolveSlotResolution(occupiedSlots, slotResolution);

        for (int i = 0; i < occupiedSlots.Count; i++) {
            Vector2Int slot = ClampSlot(occupiedSlots[i], resolution);
            JellyCube jelly = CreateJelly(color, cellCenter + GetSlotOffset(slot, resolution), parent);

            if (jelly == null) {
                continue;
            }

            jelly.occupieSlot = new List<Vector2Int> { slot };
            ResizeJelly(jelly, resolution);
            jellies.Add(jelly);
        }

        return jellies;
    }

    public void LayoutJelliesInCell(IReadOnlyList<JellyCube> jellies, Vector3 cellCenter) {
        if (jellies == null || jellies.Count == 0) {
            return;
        }

        int resolution = ResolveSlotResolution(jellies);
        int fallbackSlotIndex = 0;

        for (int i = 0; i < jellies.Count; i++) {
            JellyCube jelly = jellies[i];

            if (jelly == null) {
                continue;
            }

            Vector2Int slot = GetPrimarySlot(jelly, fallbackSlotIndex, resolution);
            fallbackSlotIndex++;

            if (jelly.occupieSlot == null || jelly.occupieSlot.Count == 0) {
                jelly.occupieSlot = new List<Vector2Int> { slot };
            }

            List<Vector2Int> slots = ClampSlots(jelly.occupieSlot, resolution);
            GetSlotBounds(slots, out int minX, out int maxX, out int minY, out int maxY);
            jelly.occupieSlot = slots;
            jelly.transform.position = cellCenter + GetSlotBoundsCenter(minX, maxX, minY, maxY, resolution);
            ResizeJellyToSlotBounds(jelly, minX, maxX, minY, maxY, resolution);
            ApplyColor(jelly, jelly.color);
        }
    }

    int ResolveSlotResolution(IReadOnlyList<JellyCube> jellies) {
        int jellyCount = 0;
        int resolution = 1;

        for (int i = 0; i < jellies.Count; i++) {
            JellyCube jelly = jellies[i];

            if (jelly == null) {
                continue;
            }

            jellyCount++;

            if (jelly.occupieSlot == null) {
                continue;
            }

            for (int slotIndex = 0; slotIndex < jelly.occupieSlot.Count; slotIndex++) {
                Vector2Int slot = jelly.occupieSlot[slotIndex];
                resolution = Mathf.Max(resolution, slot.x + 1, slot.y + 1);
            }
        }

        if (jellyCount > 1) {
            resolution = Mathf.Max(resolution, defaultSlotResolution);
        }

        while (resolution * resolution < jellyCount) {
            resolution++;
        }

        return Mathf.Max(1, resolution);
    }

    int ResolveSlotResolution(IReadOnlyList<Vector2Int> occupiedSlots, int requestedResolution) {
        int resolution = requestedResolution > 0 ? requestedResolution : defaultSlotResolution;

        for (int i = 0; i < occupiedSlots.Count; i++) {
            Vector2Int slot = occupiedSlots[i];
            resolution = Mathf.Max(resolution, slot.x + 1, slot.y + 1);
        }

        return Mathf.Max(1, resolution);
    }

    Vector2Int GetPrimarySlot(JellyCube jelly, int fallbackSlotIndex, int resolution) {
        if (jelly.occupieSlot != null && jelly.occupieSlot.Count > 0) {
            return ClampSlot(jelly.occupieSlot[0], resolution);
        }

        return new Vector2Int(fallbackSlotIndex % resolution, fallbackSlotIndex / resolution);
    }

    Vector2Int ClampSlot(Vector2Int slot, int resolution) {
        return new Vector2Int(
            Mathf.Clamp(slot.x, 0, resolution - 1),
            Mathf.Clamp(slot.y, 0, resolution - 1)
        );
    }

    Vector3 GetSlotOffset(Vector2Int slot, int resolution) {
        if (resolution <= 1) {
            return Vector3.zero;
        }

        float jellyAreaSize = GetJellyAreaSize();
        float slotSize = GetSlotSize(resolution);
        float gap = GetSlotGap();
        float firstSlotCenter = -jellyAreaSize * 0.5f + slotSize * 0.5f;

        return new Vector3(
            firstSlotCenter + slot.x * (slotSize + gap),
            firstSlotCenter + slot.y * (slotSize + gap),
            0f
        );
    }

    void ResizeJelly(JellyCube jelly, int resolution) {
        if (jelly == null) {
            return;
        }

        Renderer jellyRenderer = jelly.GetComponentInChildren<Renderer>();

        if (jellyRenderer == null) {
            jelly.transform.localScale = Vector3.one * GetSlotSize(resolution);
            return;
        }

        float currentSize = Mathf.Max(jellyRenderer.bounds.size.x, jellyRenderer.bounds.size.y, jellyRenderer.bounds.size.z);

        if (currentSize <= Mathf.Epsilon) {
            return;
        }

        float scaleFactor = GetSlotSize(resolution) / currentSize;
        jelly.transform.localScale *= scaleFactor;
    }

    JellyCube CreateJellyPart(IReadOnlyList<Vector2Int> occupiedSlots, Color color, Vector3 cellCenter, Transform parent, int resolution) {
        List<Vector2Int> slots = ClampSlots(occupiedSlots, resolution);

        if (slots.Count == 0) {
            return null;
        }

        GetSlotBounds(slots, out int minX, out int maxX, out int minY, out int maxY);

        Vector3 partCenter = cellCenter + GetSlotBoundsCenter(minX, maxX, minY, maxY, resolution);
        GameObject jellyObject = Instantiate(jellyPrefab, partCenter, jellyPrefab.transform.rotation, parent);
        JellyCube jelly = jellyObject.GetComponent<JellyCube>();

        if (jelly == null) {
            jelly = jellyObject.AddComponent<JellyCube>();
        }

        jelly.color = color;
        jelly.occupieSlot = slots;
        ApplyColor(jelly, color);
        ResizeJellyToSlotBounds(jelly, minX, maxX, minY, maxY, resolution);
        return jelly;
    }

    void ResizeJellyToSlotBounds(JellyCube jelly, int minX, int maxX, int minY, int maxY, int resolution) {
        if (jelly == null) {
            return;
        }

        Renderer jellyRenderer = jelly.GetComponentInChildren<Renderer>();

        if (jellyRenderer == null) {
            jelly.transform.localScale = GetSlotBoundsSize(minX, maxX, minY, maxY, resolution);
            return;
        }

        Vector3 currentSize = jellyRenderer.bounds.size;

        if (currentSize.x <= Mathf.Epsilon || currentSize.y <= Mathf.Epsilon || currentSize.z <= Mathf.Epsilon) {
            return;
        }

        Vector3 targetSize = GetSlotBoundsSize(minX, maxX, minY, maxY, resolution);
        jelly.transform.localScale = new Vector3(
            jelly.transform.localScale.x * targetSize.x / currentSize.x,
            jelly.transform.localScale.y * targetSize.y / currentSize.y,
            jelly.transform.localScale.z * targetSize.z / currentSize.z
        );
    }

    void ApplyColor(JellyCube jelly, Color color) {
        Renderer[] renderers = jelly.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++) {
            renderers[i].material.color = color;
        }
    }

    float GetJellyAreaSize() {
        return unitSize * jellySizeRatio;
    }

    float GetSlotGap() {
        return unitSize * jellyGapRatio;
    }

    float GetSlotSize(int resolution) {
        resolution = Mathf.Max(1, resolution);
        float totalGap = GetSlotGap() * (resolution - 1);
        return Mathf.Max(0.01f, (GetJellyAreaSize() - totalGap) / resolution);
    }

    IReadOnlyList<JellyPartShapeData> GetShapeParts(JellyShapeSO shape) {
        if (shape != null && shape.parts != null && shape.parts.Count > 0) {
            return shape.parts;
        }

        return GetFallbackShapeParts();
    }

    IReadOnlyList<JellyPartShapeData> GetFallbackShapeParts() {
        List<JellyPartShapeData> parts = new List<JellyPartShapeData>();

        if (Random.value < 0.5f) {
            parts.Add(CreatePart(Vector2Int.zero, Vector2Int.up, Vector2Int.right, Vector2Int.one));
        } else {
            parts.Add(CreatePart(Vector2Int.zero, Vector2Int.up));
            parts.Add(CreatePart(Vector2Int.right, Vector2Int.one));
        }

        return parts;
    }

    JellyPartShapeData CreatePart(params Vector2Int[] slots) {
        JellyPartShapeData part = new JellyPartShapeData();
        part.occupiedSlots.AddRange(slots);
        return part;
    }

    Color GetRandomColor() {
        if (colorTemplate != null && colorTemplate.cubeColors != null && colorTemplate.cubeColors.Count > 0) {
            Color color = colorTemplate.cubeColors[Random.Range(0, colorTemplate.cubeColors.Count)].color;
            color.a = 1f;
            return color;
        }

        return Random.ColorHSV(0f, 1f, 0.75f, 1f, 0.85f, 1f);
    }

    List<Vector2Int> ClampSlots(IReadOnlyList<Vector2Int> occupiedSlots, int resolution) {
        List<Vector2Int> slots = new List<Vector2Int>();

        for (int i = 0; i < occupiedSlots.Count; i++) {
            Vector2Int slot = ClampSlot(occupiedSlots[i], resolution);

            if (!slots.Contains(slot)) {
                slots.Add(slot);
            }
        }

        return slots;
    }

    void GetSlotBounds(IReadOnlyList<Vector2Int> slots, out int minX, out int maxX, out int minY, out int maxY) {
        minX = slots[0].x;
        maxX = slots[0].x;
        minY = slots[0].y;
        maxY = slots[0].y;

        for (int i = 1; i < slots.Count; i++) {
            Vector2Int slot = slots[i];
            minX = Mathf.Min(minX, slot.x);
            maxX = Mathf.Max(maxX, slot.x);
            minY = Mathf.Min(minY, slot.y);
            maxY = Mathf.Max(maxY, slot.y);
        }
    }

    Vector3 GetSlotBoundsCenter(int minX, int maxX, int minY, int maxY, int resolution) {
        float slotSize = GetSlotSize(resolution);
        float gap = GetSlotGap();
        float firstSlotCenter = -GetJellyAreaSize() * 0.5f + slotSize * 0.5f;

        return new Vector3(
            firstSlotCenter + (minX + maxX) * 0.5f * (slotSize + gap),
            firstSlotCenter + (minY + maxY) * 0.5f * (slotSize + gap),
            0f
        );
    }

    Vector3 GetSlotBoundsSize(int minX, int maxX, int minY, int maxY, int resolution) {
        float slotSize = GetSlotSize(resolution);
        float gap = GetSlotGap();
        int slotWidth = maxX - minX + 1;
        int slotHeight = maxY - minY + 1;

        return new Vector3(
            slotWidth * slotSize + (slotWidth - 1) * gap,
            slotHeight * slotSize + (slotHeight - 1) * gap,
            slotSize
        );
    }
}
