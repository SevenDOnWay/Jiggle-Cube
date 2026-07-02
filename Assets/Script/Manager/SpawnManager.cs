using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class SpawnManager : MonoBehaviour {

    [SerializeField] GameObject cellPrefab;
    [SerializeField] Transform jellyParent;
    [SerializeField] int spawnSlotCount = 2;
    [SerializeField] List<JellyShapeSO> spawnShapes = new List<JellyShapeSO>();
    [SerializeField] bool showSpawnPads = true;
    [SerializeField] Color spawnPadColor = new Color(0.18f, 0.16f, 0.28f, 1f);
    [SerializeField] float spawnPadSizeRatio = 1.45f;
    [SerializeField] float spawnPadDepthRatio = 0.06f;


    readonly List<GameObject> spawnedSlotRoots = new List<GameObject>();
    readonly List<JellyPiece> availablePieces = new List<JellyPiece>();

    PlayScreen playScreen;
    JellyCubeFactory jellyCubeFactory;

    #region API

    [Inject]
    public void Construct(PlayScreen playScreen, JellyCubeFactory jellyCubeFactory) {
        this.playScreen = playScreen;
        this.jellyCubeFactory = jellyCubeFactory;
    }

    public void SpawnGridVisuals(GridManager gridManager, LevelSO level) {
        if (gridManager == null) {
            return;
        }

        gridManager.ClearGridVisualCells();

        if (cellPrefab == null) {
            Debug.LogWarning("SpawnManager needs a cellPrefab before it can build visual grid cells.", this);
            return;
        }

        Transform parent = gridManager.transform;

        for (int x = 0; x < gridManager.Width; x++) {
            for (int y = 0; y < gridManager.Height; y++) {
                Vector2Int gridPosition = new Vector2Int(x, y);

                if (level != null && level.IsBlocked(gridPosition)) {
                    continue;
                }

                Vector3 worldPosition = gridManager.GridToWorldPosition(gridPosition) + Vector3.forward * gridManager.CellVisualZOffset;
                GameObject cellVisual = Instantiate(cellPrefab, worldPosition, cellPrefab.transform.rotation, parent);

                cellVisual.name = $"Cell ({x}, {y})";
                ResizeToCell(cellVisual);
                gridManager.AddCellVisual(cellVisual);
            }
        }
    }

    public void SpawnPlayerJellies() {
        spawnSlotCount = Mathf.Max(1, spawnSlotCount);
        ClearSpawnedSlots();

        for (int i = 0; i < spawnSlotCount; i++) {
            spawnedSlotRoots.Add(null);
            RefillSpawnSlot(i);
        }
    }

    public IReadOnlyList<JellyPiece> GetAvailableJellyPieces() {
        availablePieces.Clear();

        for (int i = 0; i < spawnedSlotRoots.Count; i++) {
            GameObject slotRoot = spawnedSlotRoots[i];

            if (slotRoot == null || !slotRoot.TryGetComponent(out JellyPiece piece)) {
                continue;
            }

            availablePieces.Add(piece);
        }

        return availablePieces;
    }

    public void CompletePlacedPiece(JellyPiece piece) {
        if (piece == null) {
            return;
        }

        int spawnSlotIndex = piece.SpawnSlotIndex;

        if (spawnSlotIndex >= 0 &&
            spawnSlotIndex < spawnedSlotRoots.Count &&
            spawnedSlotRoots[spawnSlotIndex] == piece.gameObject) {
            spawnedSlotRoots[spawnSlotIndex] = null;
        }

        piece.MarkPlaced(jellyParent);
        RefillSpawnSlot(spawnSlotIndex);
    }

    #endregion

    #region Internal

    void RefillSpawnSlot(int slotIndex) {
        if (playScreen == null || jellyCubeFactory == null || slotIndex < 0 || slotIndex >= spawnedSlotRoots.Count) {
            return;
        }

        DestroySpawnSlot(slotIndex);

        Vector3 spawnPosition = playScreen.GetSpawnPosition(slotIndex, spawnSlotCount);
        GameObject root = new GameObject($"Jelly Spawn Slot {slotIndex + 1}");
        root.transform.SetParent(jellyParent != null ? jellyParent : transform, false);
        root.transform.position = spawnPosition;
        spawnedSlotRoots[slotIndex] = root;

        CreateSpawnPad(root.transform);

        JellyShapeSO shape = GetRandomShape();
        List<JellyCube> jellies = jellyCubeFactory.CreateJellyShape(shape, spawnPosition, root.transform);
        JellyPiece piece = root.AddComponent<JellyPiece>();
        piece.Initialize(slotIndex, jellies);
    }

    void CreateSpawnPad(Transform root) {
        if (!showSpawnPads || root == null || playScreen == null) {
            return;
        }

        float unitSize = playScreen.CellVisualSize;
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "Spawn Pad";
        pad.transform.SetParent(root, false);
        pad.transform.localPosition = Vector3.back * (unitSize * spawnPadDepthRatio);
        pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        pad.transform.localScale = new Vector3(unitSize * spawnPadSizeRatio, unitSize * spawnPadDepthRatio, unitSize * spawnPadSizeRatio);

        Collider padCollider = pad.GetComponent<Collider>();

        if (padCollider != null) {
            Destroy(padCollider);
        }

        Renderer padRenderer = pad.GetComponent<Renderer>();

        if (padRenderer != null) {
            padRenderer.material.color = spawnPadColor;
        }
    }

    void ResizeToCell(GameObject cellVisual) {
        if (cellVisual == null || playScreen == null) {
            return;
        }

        Renderer cellRenderer = cellVisual.GetComponentInChildren<Renderer>();

        if (cellRenderer == null) {
            cellVisual.transform.localScale = Vector3.one * playScreen.CellVisualSize;
            return;
        }

        float currentSize = Mathf.Max(cellRenderer.bounds.size.x, cellRenderer.bounds.size.y);

        if (currentSize <= Mathf.Epsilon) {
            return;
        }

        float scaleFactor = playScreen.CellVisualSize / currentSize;
        cellVisual.transform.localScale *= scaleFactor;
    }

    void ClearSpawnedSlots() {
        for (int i = spawnedSlotRoots.Count - 1; i >= 0; i--) {
            DestroySpawnSlot(i);
        }

        spawnedSlotRoots.Clear();
    }

    void DestroySpawnSlot(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= spawnedSlotRoots.Count) {
            return;
        }

        GameObject slotRoot = spawnedSlotRoots[slotIndex];

        if (slotRoot == null) {
            return;
        }

        if (Application.isPlaying) {
            Destroy(slotRoot);
        } else {
            DestroyImmediate(slotRoot);
        }

        spawnedSlotRoots[slotIndex] = null;
    }

    JellyShapeSO GetRandomShape() {
        if (spawnShapes != null && spawnShapes.Count > 0) {
            List<JellyShapeSO> validShapes = new List<JellyShapeSO>();

            for (int i = 0; i < spawnShapes.Count; i++) {
                if (spawnShapes[i] != null && spawnShapes[i].parts != null && spawnShapes[i].parts.Count > 0) {
                    validShapes.Add(spawnShapes[i]);
                }
            }

            if (validShapes.Count > 0) {
                return validShapes[Random.Range(0, validShapes.Count)];
            }
        }

        return null;
    }

    #endregion
}
