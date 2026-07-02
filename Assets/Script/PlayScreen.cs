using UnityEngine;

public class PlayScreen {
    public int row { get; private set; }
    public int collumn { get; private set; }
    public float squareSize { get; private set; }
    public float WorldWidth { get; private set; }
    public float WorldHeight { get; private set; }
    public float CellVisualSize => squareSize;
    public float CellSpacing { get; private set; }
    public float CellVisualZOffset => CellVisualSize * CellVisualZOffsetRatio;
    public float GridStep => CellVisualSize + CellSpacing;
    public int CellSlotResolution { get; private set; }
    public float CellSpacingRatio { get; private set; }
    public float CellVisualZOffsetRatio { get; private set; }
    public float JellySizeRatio { get; private set; }
    public float JellyGapRatio { get; private set; }
    public Vector3 GridCenter { get; private set; }
    public Vector3 SpawnAreaCenter { get; private set; }
    public Vector2 SpawnAreaSize { get; private set; }

    public PlayScreen(
        Camera camera,
        int column,
        int row,
        int cellSlotResolution,
        float padding,
        float spawnAreaRatio,
        float gridSpawnGapRatio,
        float cellSpacingRatio,
        float cellVisualZOffsetRatio,
        float jellySizeRatio,
        float jellyGapRatio
    ) {
        CellSlotResolution = Mathf.Max(1, cellSlotResolution);
        CellVisualZOffsetRatio = Mathf.Max(0f, cellVisualZOffsetRatio);
        JellySizeRatio = Mathf.Clamp(jellySizeRatio, 0.1f, 1f);
        JellyGapRatio = Mathf.Max(0f, jellyGapRatio);

        CalculateLayout(camera, column, row, padding, spawnAreaRatio, gridSpawnGapRatio, cellSpacingRatio);
    }

    void CalculateLayout(Camera camera, int column, int row, float padding, float spawnAreaRatio, float gridSpawnGapRatio, float cellSpacingRatio) {
        column = Mathf.Max(1, column);
        row = Mathf.Max(1, row);
        padding = Mathf.Clamp01(padding);
        spawnAreaRatio = Mathf.Clamp(spawnAreaRatio, 0.1f, 0.45f);
        gridSpawnGapRatio = Mathf.Clamp(gridSpawnGapRatio, 0f, 0.15f);
        cellSpacingRatio = Mathf.Max(0f, cellSpacingRatio);
        CellSpacingRatio = cellSpacingRatio;

        GetOrthographicWorldBounds(camera, out float left, out float right, out float bottom, out float top);

        WorldWidth = right - left;
        WorldHeight = top - bottom;

        float contentWidth = WorldWidth * padding;
        float contentHeight = WorldHeight * padding;
        float verticalMargin = (WorldHeight - contentHeight) * 0.5f;
        float spawnAreaHeight = WorldHeight * spawnAreaRatio;
        float gap = WorldHeight * gridSpawnGapRatio;
        float gridAvailableHeight = Mathf.Max(0.1f, contentHeight - spawnAreaHeight - gap);
        float gridWorldSize = Mathf.Min(contentWidth, gridAvailableHeight);
        int largestGridAxis = Mathf.Max(column, row);

        squareSize = gridWorldSize / (largestGridAxis + cellSpacingRatio * (largestGridAxis - 1));
        CellSpacing = squareSize * cellSpacingRatio;

        float centerX = (left + right) * 0.5f;
        float layoutZ = 0f;
        float spawnBottom = bottom + verticalMargin;
        float spawnTop = spawnBottom + spawnAreaHeight;
        float gridBottom = spawnTop + gap;
        float gridTop = top - verticalMargin;

        GridCenter = new Vector3(centerX, (gridBottom + gridTop) * 0.5f, layoutZ);
        SpawnAreaCenter = new Vector3(centerX, (spawnBottom + spawnTop) * 0.5f, layoutZ);
        SpawnAreaSize = new Vector2(contentWidth, spawnAreaHeight);

        Debug.Log($"PlayScreen Layout - World: {WorldWidth}x{WorldHeight}, Cell: {CellVisualSize}, Spacing: {CellSpacing}, GridCenter: {GridCenter}, SpawnCenter: {SpawnAreaCenter}");
    }

    void GetOrthographicWorldBounds(Camera camera, out float left, out float right, out float bottom, out float top) {
        float worldHeight = camera != null ? camera.orthographicSize * 2f : 10f;
        float aspect = camera != null ? camera.aspect : 1f;
        float worldWidth = worldHeight * aspect;
        Vector3 cameraPosition = camera != null ? camera.transform.position : Vector3.zero;

        left = cameraPosition.x - worldWidth * 0.5f;
        right = cameraPosition.x + worldWidth * 0.5f;
        bottom = cameraPosition.y - worldHeight * 0.5f;
        top = cameraPosition.y + worldHeight * 0.5f;
    }

    public Vector3 GetSpawnPosition(int index, int totalSlots) {
        totalSlots = Mathf.Max(1, totalSlots);

        float slotWidth = SpawnAreaSize.x / totalSlots;
        float x = SpawnAreaCenter.x - SpawnAreaSize.x * 0.5f + slotWidth * (index + 0.5f);
        return new Vector3(x, SpawnAreaCenter.y, SpawnAreaCenter.z);
    }

    public float GetSquareSize() => squareSize;

}
