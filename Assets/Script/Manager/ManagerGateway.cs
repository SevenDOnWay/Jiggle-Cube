using UnityEngine;
using VContainer;

public class ManagerGateway : MonoBehaviour {
    GridManager gridManager;
    SpawnManager spawnManager;
    Controller controller;


    [Inject]
    public void Construct(GridManager gridManager, SpawnManager spawnManager, Controller controller) {
        this.gridManager = gridManager;
        this.spawnManager = spawnManager;
        this.controller = controller;
    }

    void Start() {
        if (controller != null) {
            controller.PlacementRequested += OnPlacementRequested;
        }

        BuildLevel(gridManager != null ? gridManager.StartingLevel : null);
    }

    void OnDestroy() {
        if (controller != null) {
            controller.PlacementRequested -= OnPlacementRequested;
        }
    }

    public void BuildLevel(LevelSO level) {
        if (gridManager == null || spawnManager == null) {
            return;
        }

        gridManager.BuildGrid(level);
        spawnManager.SpawnGridVisuals(gridManager, level);
        spawnManager.SpawnPlayerJellies();
    }

    void OnPlacementRequested(JellyPiece jelly, Vector3 worldPosition, System.Action<bool> complete) {
        bool placed = TryPlaceJellyPiece(jelly, worldPosition);
        complete?.Invoke(placed);
    }

    bool TryPlaceJellyPiece(JellyPiece jelly, Vector3 worldPosition) {
        if (gridManager == null || spawnManager == null || jelly == null) {
            return false;
        }

        if (!gridManager.TryGetCellPosition(worldPosition, out Vector2Int cellPosition) ||
            !gridManager.TryPlacePiece(jelly, cellPosition)) {
            return false;
        }

        spawnManager.CompletePlacedPiece(jelly);
        Debug.Log($"Jelly piece placed at {cellPosition}");
        return true;
    }
    

}
