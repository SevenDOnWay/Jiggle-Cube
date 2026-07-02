using UnityEngine;
using System.Collections.Generic;
using VContainer;
using JiggleCube.Gameplay;

public class GridManager : MonoBehaviour {

    PlayScreen playScreen;

    float squareSize = 1f;

    GridCell[,] grid;
    readonly List<JellyCube> placedJellies = new List<JellyCube>();

    readonly int[] dx = { 0, -1, 0, 1 };
    readonly int[] dy = { 1, 0, -1, 0 };

    const int maxWidth = 6;
    const int maxHeight = 6;

    [SerializeField] bool clearConnectedJellies = true;
    [SerializeField] int minimumConnectionSize = 2;
    [SerializeField] List<Vector2Int> blockedCells = new List<Vector2Int>();
    [SerializeField] LevelSO startingLevel;
    [SerializeField] CubeColorSO colorPalette;
    [SerializeField] JellyCubeFactory jellyCubeFactory;
    [SerializeField] Transform jellyParent;
    readonly List<GameObject> spawnedCellVisuals = new List<GameObject>();  

    public int Width => maxWidth;
    public int Height => maxHeight;
    public LevelSO StartingLevel => startingLevel;
    public float CellVisualZOffset => GetCellVisualZOffset();

    [Inject]
    public void Construct(PlayScreen playScreen, JellyCubeFactory jellyCubeFactory) {
        this.playScreen = playScreen;
        this.jellyCubeFactory = jellyCubeFactory;
    }

    public void Start() {
    }

    public void BuildGrid(LevelSO level = null) {
        if (playScreen != null) {
            squareSize = playScreen.squareSize;
            transform.position = playScreen.GridCenter;
        }

        InitializeEmptyGrid(level != null ? level : startingLevel);
    }

    
    //TODO: Use LevelSO to load level
    public void LoadLevel(LevelSO level, CubeColorSO palette) {
        colorPalette = palette;
        //LoadLevel(level);
    }

    public GridCell GetCell(Vector2Int position) {
        EnsureGridLoaded();
        return IsInsideGrid(position) ? grid[position.x, position.y] : null;
    }

    public bool CanPlaceCell(IReadOnlyList<JellyCube> jellies, Vector2Int cellPosition) {
        EnsureGridLoaded();

        if (jellies == null || jellies.Count == 0 || !IsInsideGrid(cellPosition)) {
            return false;
        }

        bool hasValidJelly = false;

        for (int i = 0; i < jellies.Count; i++) {
            if (jellies[i] != null) {
                hasValidJelly = true;
                break;
            }
        }

        if (!hasValidJelly) {
            return false;
        }

        GridCell targetCell = grid[cellPosition.x, cellPosition.y];
        return CanCellAcceptJellies(targetCell, jellies);
    }

    public bool TryPlaceCell(IReadOnlyList<JellyCube> jellies, Vector2Int cellPosition) {
        if (!CanPlaceCell(jellies, cellPosition)) {
            return false;
        }

        GridCell targetCell = grid[cellPosition.x, cellPosition.y];

        for (int i = 0; i < jellies.Count; i++) {
            JellyCube jelly = jellies[i];

            if (jelly == null) {
                continue;
            }

            RemoveJellyFromCurrentCell(jelly);
            jelly.cellPos = cellPosition;

            if (!placedJellies.Contains(jelly)) {
                placedJellies.Add(jelly);
            }
        }

        targetCell.SetJellies(jellies);
        GetFactory().LayoutJelliesInCell(targetCell.Cubes, GridToWorldPosition(cellPosition));
        CheckConnect(cellPosition);
        return true;
    }

    public bool TryPlacePiece(JellyPiece piece, Vector2Int cellPosition) {
        return piece != null && TryPlaceCell(piece.Jellies, cellPosition);
    }

    public bool TryPlaceCube() {
        Debug.LogWarning("TryPlaceCube() needs a jelly or a cell-pattern and a target cell. Use TryPlaceCube(JellyCube jelly, Vector2Int cellPosition) or TryPlaceCell(...).");
        return false;
    }

    public bool TryPlaceCube(JellyCube jelly, Vector2Int cellPosition) {
        if (jelly == null) {
            return false;
        }

        return TryPlaceCell(new List<JellyCube> { jelly }, cellPosition);
    }
    

    //Entry point: conttroller

    public bool TryPlaceCubeAtWorldPosition(JellyCube jelly, Vector3 worldPosition) {
        if (jelly == null || !TryGetCellPosition(worldPosition, out Vector2Int cellPosition)) {
            return false;
        }

        return TryPlaceCube(jelly, cellPosition);
    }

    public bool TryPlaceCubeAtScreenPosition(JellyCube jelly, Vector2 screenPosition, Camera camera) {
        if (jelly == null || camera == null) {
            return false;
        }

        Ray ray = camera.ScreenPointToRay(screenPosition);
        Plane cellPlane = new Plane(Vector3.forward, transform.position + Vector3.forward * GetCellVisualZOffset());

        if (!cellPlane.Raycast(ray, out float enter)) {
            return false;
        }

        return TryPlaceCubeAtWorldPosition(jelly, ray.GetPoint(enter));
    }

    public bool TryGetCellPosition(Vector3 worldPosition, out Vector2Int cellPosition) {
        EnsureGridLoaded();

        float step = GetGridStep();
        float halfCellSize = GetCellVisualSize() * 0.5f;
        Vector3 localPosition = worldPosition - transform.position;
        Vector2 centerOffset = GetGridCenterOffset();

        cellPosition = new Vector2Int(
            Mathf.RoundToInt(localPosition.x / step + centerOffset.x),
            Mathf.RoundToInt(localPosition.y / step + centerOffset.y)
        );

        if (!IsInsideGrid(cellPosition)) {
            return false;
        }

        Vector2 cellCenter = new Vector2(
            (cellPosition.x - centerOffset.x) * step,
            (cellPosition.y - centerOffset.y) * step
        );
        Vector2 deltaFromCenter = new Vector2(localPosition.x, localPosition.y) - cellCenter;

        return Mathf.Abs(deltaFromCenter.x) <= halfCellSize && Mathf.Abs(deltaFromCenter.y) <= halfCellSize;
    }

    public void CheckConnect(Vector2Int pos) {
        EnsureGridLoaded();

        if (!IsInsideGrid(pos) || grid[pos.x, pos.y].State == GridCellState.Blocked) {
            return;
        }

        List<List<JellyCube>> connectedGroups = FindConnectedGroups(pos);
        List<JellyCube> jelliesToClear = new List<JellyCube>();

        for (int i = 0; i < connectedGroups.Count; i++) {
            List<JellyCube> group = connectedGroups[i];

            if (group.Count < minimumConnectionSize) {
                continue;
            }

            for (int j = 0; j < group.Count; j++) {
                if (!jelliesToClear.Contains(group[j])) {
                    jelliesToClear.Add(group[j]);
                }
            }
        }

        if (clearConnectedJellies && jelliesToClear.Count > 0) {
            ClearJellyCubes(jelliesToClear);
        }
    }

    public List<JellyCube> FindConnectedCubes(Vector2Int pos) {
        List<JellyCube> largestGroup = new List<JellyCube>();
        List<List<JellyCube>> groups = FindConnectedGroups(pos);

        for (int i = 0; i < groups.Count; i++) {
            if (groups[i].Count > largestGroup.Count) {
                largestGroup = groups[i];
            }
        }

        return largestGroup;
    }

    public List<List<JellyCube>> FindConnectedGroups(Vector2Int pos) {
        List<List<JellyCube>> groups = new List<List<JellyCube>>();

        if (!IsInsideGrid(pos)) {
            return groups;
        }

        List<JellyCube> startJellies = grid[pos.x, pos.y].Cubes;
        HashSet<JellyCube> visited = new HashSet<JellyCube>();

        for (int i = 0; i < startJellies.Count; i++) {
            JellyCube startJelly = startJellies[i];

            if (startJelly == null || visited.Contains(startJelly)) {
                continue;
            }

            List<JellyCube> group = FloodFillSameColor(startJelly, visited);
            groups.Add(group);
        }

        return groups;
    }

    List<JellyCube> FloodFillSameColor(JellyCube startJelly, HashSet<JellyCube> visited) {
        List<JellyCube> group = new List<JellyCube>();
        Queue<JellyCube> queue = new Queue<JellyCube>();

        queue.Enqueue(startJelly);
        visited.Add(startJelly);

        while (queue.Count > 0) {
            JellyCube currentJelly = queue.Dequeue();
            group.Add(currentJelly);

            foreach (JellyCube neighborJelly in GetTouchingSameColorNeighbors(currentJelly)) {
                if (neighborJelly == null || visited.Contains(neighborJelly)) {
                    continue;
                }

                visited.Add(neighborJelly);
                queue.Enqueue(neighborJelly);
            }
        }

        return group;
    }

    IEnumerable<JellyCube> GetTouchingSameColorNeighbors(JellyCube jelly) {
        Vector2Int currentPos = jelly.cellPos;

        if (!IsInsideGrid(currentPos)) {
            yield break;
        }

        List<JellyCube> sameCellJellies = grid[currentPos.x, currentPos.y].Cubes;

        for (int i = 0; i < sameCellJellies.Count; i++) {
            JellyCube otherJelly = sameCellJellies[i];

            if (CanConnect(jelly, otherJelly) && TouchesInsideSameCell(jelly, otherJelly)) {
                yield return otherJelly;
            }
        }

        for (int i = 0; i < dx.Length; i++) {
            Vector2Int nextPos = new Vector2Int(currentPos.x + dx[i], currentPos.y + dy[i]);

            if (!IsInsideGrid(nextPos) || grid[nextPos.x, nextPos.y].State == GridCellState.Blocked) {
                continue;
            }

            List<JellyCube> neighborCellJellies = grid[nextPos.x, nextPos.y].Cubes;

            for (int jellyIndex = 0; jellyIndex < neighborCellJellies.Count; jellyIndex++) {
                JellyCube otherJelly = neighborCellJellies[jellyIndex];

                if (CanConnect(jelly, otherJelly) && TouchesAcrossCells(jelly, otherJelly, i)) {
                    yield return otherJelly;
                }
            }
        }
    }

    bool CanConnect(JellyCube first, JellyCube second) {
        if (first == null || second == null || first == second) {
            return false;
        }

        //if (!string.IsNullOrEmpty(first.colorId) || !string.IsNullOrEmpty(second.colorId)) {
        //    return first.colorId == second.colorId;
        //}

        return first.color == second.color;
    }

    bool TouchesInsideSameCell(JellyCube first, JellyCube second) {
        IReadOnlyList<Vector2Int> firstSlots = GetOccupiedSlots(first);
        IReadOnlyList<Vector2Int> secondSlots = GetOccupiedSlots(second);

        for (int i = 0; i < firstSlots.Count; i++) {
            for (int j = 0; j < secondSlots.Count; j++) {
                Vector2Int delta = firstSlots[i] - secondSlots[j];

                if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1) {
                    return true;
                }
            }
        }

        return false;
    }

    bool TouchesAcrossCells(JellyCube currentJelly, JellyCube otherJelly, int directionIndex) {
        IReadOnlyList<Vector2Int> currentSlots = GetOccupiedSlots(currentJelly);
        IReadOnlyList<Vector2Int> otherSlots = GetOccupiedSlots(otherJelly);
        int slotResolution = GetCellSlotResolution();

        for (int i = 0; i < currentSlots.Count; i++) {
            for (int j = 0; j < otherSlots.Count; j++) {
                if (SlotsTouchAcrossCells(currentSlots[i], otherSlots[j], directionIndex, slotResolution, slotResolution)) {
                    return true;
                }
            }
        }

        return false;
    }

    bool SlotsTouchAcrossCells(Vector2Int currentSlot, Vector2Int otherSlot, int directionIndex, int currentResolution, int otherResolution) {
        if (currentResolution != otherResolution) {
            return false;
        }

        if (directionIndex == 0) {
            return currentSlot.y == currentResolution - 1 && otherSlot.y == 0 && currentSlot.x == otherSlot.x;
        }

        if (directionIndex == 1) {
            return currentSlot.x == 0 && otherSlot.x == otherResolution - 1 && currentSlot.y == otherSlot.y;
        }

        if (directionIndex == 2) {
            return currentSlot.y == 0 && otherSlot.y == otherResolution - 1 && currentSlot.x == otherSlot.x;
        }

        if (directionIndex == 3) {
            return currentSlot.x == currentResolution - 1 && otherSlot.x == 0 && currentSlot.y == otherSlot.y;
        }

        return false;
    }

    public void ClearJellyCubes(IReadOnlyList<JellyCube> jelliesToClear) {
        if (jelliesToClear == null) {
            return;
        }

        for (int i = 0; i < jelliesToClear.Count; i++) {
            JellyCube jelly = jelliesToClear[i];

            if (jelly == null) {
                continue;
            }

            RemoveJellyFromCurrentCell(jelly);
            placedJellies.Remove(jelly);
            DestroyJelly(jelly);
        }
    }

    void InitializeEmptyGrid(LevelSO level) {
        ClearRuntimeJellies();

        grid = new GridCell[maxWidth, maxHeight];

        for (int x = 0; x < maxWidth; x++) {
            for (int y = 0; y < maxHeight; y++) {
                grid[x, y] = new GridCell(new Vector2Int(x, y));
            }
        }

        ApplyBlockedCells(level);
    }

    void ApplyBlockedCells(LevelSO level) {
        if (level != null && level.blockedCells != null) {
            for (int i = 0; i < level.blockedCells.Count; i++) {
                SetBlockedCell(level.blockedCells[i]);
            }

            return;
        }

        for (int i = 0; i < blockedCells.Count; i++) {
            SetBlockedCell(blockedCells[i]);
        }
    }

    void SetBlockedCell(Vector2Int position) {
        if (IsInsideGrid(position)) {
            grid[position.x, position.y].SetBlocked(true);
        }
    }

    void ClearRuntimeJellies() {
        for (int i = placedJellies.Count - 1; i >= 0; i--) {
            JellyCube jelly = placedJellies[i];

            if (jelly != null) {
                DestroyJelly(jelly);
            }
        }

        placedJellies.Clear();
    }

    public void AddCellVisual(GameObject cellVisual) {
        if (cellVisual != null && !spawnedCellVisuals.Contains(cellVisual)) {
            spawnedCellVisuals.Add(cellVisual);
        }
    }

    public void ClearGridVisualCells() {
        for (int i = spawnedCellVisuals.Count - 1; i >= 0; i--) {
            GameObject cellVisual = spawnedCellVisuals[i];

            if (cellVisual == null) {
                continue;
            }

            if (Application.isPlaying) {
                Destroy(cellVisual);
            } else {
                DestroyImmediate(cellVisual);
            }
        }

        spawnedCellVisuals.Clear();
    }

    void RemoveJellyFromCurrentCell(JellyCube jelly) {
        if (jelly == null || !IsInsideGrid(jelly.cellPos) || grid == null) {
            return;
        }

        grid[jelly.cellPos.x, jelly.cellPos.y].RemoveJelly(jelly);
    }

    bool CanCellAcceptJellies(GridCell cell, IReadOnlyList<JellyCube> jellies) {
        if (cell == null || !cell.IsPlayable) {
            return false;
        }

        for (int i = 0; i < cell.Cubes.Count; i++) {
            JellyCube existingJelly = cell.Cubes[i];

            if (existingJelly != null && !ContainsJelly(jellies, existingJelly)) {
                return false;
            }
        }

        return true;
    }

    bool ContainsJelly(IReadOnlyList<JellyCube> jellies, JellyCube jelly) {
        if (jellies == null || jelly == null) {
            return false;
        }

        for (int i = 0; i < jellies.Count; i++) {
            if (jellies[i] == jelly) {
                return true;
            }
        }

        return false;
    }

    IReadOnlyList<Vector2Int> GetOccupiedSlots(JellyCube jelly) {
        if (jelly == null || jelly.occupieSlot == null) {
            return new List<Vector2Int>();
        }

        return jelly.occupieSlot;
    }

    bool IsInsideGrid(Vector2Int position) {
        return position.x >= 0 && position.x < maxWidth && position.y >= 0 && position.y < maxHeight;
    }

    public Vector3 GridToWorldPosition(Vector2Int position) {
        float size = GetGridStep();
        Vector2 centerOffset = GetGridCenterOffset();
        return transform.position + new Vector3((position.x - centerOffset.x) * size, (position.y - centerOffset.y) * size, 0f);
    }

    float GetGridStep() {
        return playScreen != null ? playScreen.GridStep : GetCellVisualSize();
    }

    Vector2 GetGridCenterOffset() {
        return new Vector2((maxWidth - 1) * 0.5f, (maxHeight - 1) * 0.5f);
    }

    float GetCellVisualSize() {
        return playScreen != null ? playScreen.CellVisualSize : Mathf.Max(squareSize, 1f);
    }

    float GetCellVisualZOffset() {
        return playScreen != null ? playScreen.CellVisualZOffset : GetCellVisualSize() * 0.5f;
    }

    int GetCellSlotResolution() {
        return playScreen != null ? Mathf.Max(1, playScreen.CellSlotResolution) : 2;
    }

    JellyCubeFactory GetFactory() {
        if (jellyCubeFactory != null) {
            return jellyCubeFactory;
        }

        jellyCubeFactory = GetComponent<JellyCubeFactory>();

        if (jellyCubeFactory == null) {
            jellyCubeFactory = gameObject.AddComponent<JellyCubeFactory>();
        }

        return jellyCubeFactory;
    }

    void DestroyJelly(JellyCube jelly) {
        if (Application.isPlaying) {
            Destroy(jelly.gameObject);
        } else {
            DestroyImmediate(jelly.gameObject);
        }
    }

    void EnsureGridLoaded() {
        if (grid == null) {
            BuildGrid();
        }
    }
}
