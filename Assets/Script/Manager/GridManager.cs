using UnityEngine;
using System.Collections.Generic;
using VContainer;
using JiggleCube.Gameplay;

public class GridManager : MonoBehaviour {

    PlayScreen playScreen;

    float squareSize;

    GridCell[,] grid;
    readonly List<JellyCube> placedJellies = new List<JellyCube>();

    readonly int[] dx = { 0, -1, 0, 1 };
    readonly int[] dy = { 1, 0, -1, 0 };

    const int maxWidth = 6;
    const int maxHeight = 6;

    [SerializeField] int cellSlotResolution = 2;
    [SerializeField] int minimumConnectionSize = 3;
    [SerializeField] bool clearConnectedJellies = true;
    [SerializeField] List<Vector2Int> blockedCells = new List<Vector2Int>();

    [Inject]
    public void Construct(PlayScreen playScreen) {
        this.playScreen = playScreen;
    }

    public void Start() {
        squareSize = playScreen.squareSize;
        EnsureGridLoaded();
    }

    // TODO: replace blockedCells with a LevelSO once level authoring is ready.
    public void LoadNewLevel() {
        grid = new GridCell[maxWidth, maxHeight];
        placedJellies.Clear();

        for (int x = 0; x < maxWidth; x++) {
            for (int y = 0; y < maxHeight; y++) {
                grid[x, y] = new GridCell(new Vector2Int(x, y));
            }
        }

        for (int i = 0; i < blockedCells.Count; i++) {
            Vector2Int blockedPosition = blockedCells[i];

            if (!IsInsideGrid(blockedPosition)) {
                continue;
            }

            grid[blockedPosition.x, blockedPosition.y].SetBlocked(true);
        }
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
        return targetCell.CanAcceptJelly;
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
            jelly.transform.position = GridToWorldPosition(cellPosition);

            if (!placedJellies.Contains(jelly)) {
                placedJellies.Add(jelly);
            }
        }

        targetCell.SetJellies(jellies);
        CheckConnect(cellPosition);
        return true;
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
        return first != null && second != null && first != second && first.color == second.color;
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

        for (int i = 0; i < currentSlots.Count; i++) {
            for (int j = 0; j < otherSlots.Count; j++) {
                if (SlotsTouchAcrossCells(currentSlots[i], otherSlots[j], directionIndex)) {
                    return true;
                }
            }
        }

        return false;
    }

    bool SlotsTouchAcrossCells(Vector2Int currentSlot, Vector2Int otherSlot, int directionIndex) {
        if (directionIndex == 0) {
            return currentSlot.y == cellSlotResolution - 1 && otherSlot.y == 0 && currentSlot.x == otherSlot.x;
        }

        if (directionIndex == 1) {
            return currentSlot.x == 0 && otherSlot.x == cellSlotResolution - 1 && currentSlot.y == otherSlot.y;
        }

        if (directionIndex == 2) {
            return currentSlot.y == 0 && otherSlot.y == cellSlotResolution - 1 && currentSlot.x == otherSlot.x;
        }

        if (directionIndex == 3) {
            return currentSlot.x == cellSlotResolution - 1 && otherSlot.x == 0 && currentSlot.y == otherSlot.y;
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
            Destroy(jelly.gameObject);
        }
    }

    void RemoveJellyFromCurrentCell(JellyCube jelly) {
        if (jelly == null || !IsInsideGrid(jelly.cellPos)) {
            return;
        }

        grid[jelly.cellPos.x, jelly.cellPos.y].RemoveJelly(jelly);
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

    Vector3 GridToWorldPosition(Vector2Int position) {
        return new Vector3(position.x * squareSize, position.y * squareSize, 0f);
    }

    void EnsureGridLoaded() {
        if (grid == null) {
            LoadNewLevel();
        }
    }
}
