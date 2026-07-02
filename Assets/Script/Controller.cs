using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class Controller : MonoBehaviour {

    [SerializeField] InputActionAsset mouseInputSystem;
    [SerializeField] float speed = 1f;

    public event Action<JellyPiece, Vector3, Action<bool>> PlacementRequested;

    InputAction onPress;
    Camera mainCamera;
    SpawnManager spawnManager;
    readonly Dictionary<Transform, JellyPiece> jelly = new Dictionary<Transform, JellyPiece>();
    JellyPiece activeJelly;
    bool pressedOnThisCube;
    Vector3 dragOffset;
    Vector3 dragStartPosition;

    [Inject]
    public void Construct(SpawnManager spawnManager) {
        this.spawnManager = spawnManager;
    }

    public void ReleaseJelly() {
        activeJelly = null;
        jelly.Clear();
    }

    void OnEnable() {
        mainCamera = Camera.main;

        InputActionMap gameplayMap = mouseInputSystem.FindActionMap("Gameplay", true);
        onPress = gameplayMap.FindAction("Move Object", true);
        gameplayMap.Enable();
    }

    void OnDisable() {
        mouseInputSystem.FindActionMap("Gameplay")?.Disable();
    }

    void Update() {
        SyncJellyPieces();

        if (jelly.Count == 0) {
            return;
        }

        if (onPress.WasPressedThisFrame()) {
            pressedOnThisCube = TryGetMouseJellyPiece(out activeJelly);

            if (pressedOnThisCube) {
                dragStartPosition = GetDragRoot().position;

                if (TryGetMouseWorldPosition(out Vector3 mouseWorldPosition)) {
                    dragOffset = GetDragRoot().position - mouseWorldPosition;
                } else {
                    dragOffset = Vector3.zero;
                }
            }
        }

        if (onPress.WasReleasedThisFrame()) {
            if (pressedOnThisCube) {
                PlaceOnGridOrReturn();
            }

            pressedOnThisCube = false;
        }

        if (pressedOnThisCube && onPress.IsPressed()) {
            if (TryGetMouseWorldPosition(out Vector3 mouseWorldPosition)) {
                GetDragRoot().position = mouseWorldPosition + dragOffset;
            } else {
                Vector2 mouseDir = Mouse.current.delta.ReadValue();
                GetDragRoot().position += (Vector3)mouseDir * speed * Time.deltaTime;
            }
        }
    }

    void SyncJellyPieces() {
        jelly.Clear();

        if (spawnManager == null) {
            return;
        }

        IReadOnlyList<JellyPiece> availablePieces = spawnManager.GetAvailableJellyPieces();

        for (int i = 0; i < availablePieces.Count; i++) {
            RegisterJellyPiece(availablePieces[i]);
        }

        RegisterJellyPiece(activeJelly);
    }

    void RegisterJellyPiece(JellyPiece piece) {
        if (piece == null) {
            return;
        }

        jelly[piece.transform] = piece;

        for (int i = 0; i < piece.Jellies.Count; i++) {
            JellyCube cube = piece.Jellies[i];

            if (cube == null) {
                continue;
            }

            jelly[cube.transform] = piece;

            Transform[] childTransforms = cube.GetComponentsInChildren<Transform>();

            for (int childIndex = 0; childIndex < childTransforms.Length; childIndex++) {
                jelly[childTransforms[childIndex]] = piece;
            }
        }
    }

    bool TryGetMouseJellyPiece(out JellyPiece piece) {
        piece = null;

        if (mainCamera == null || Mouse.current == null) {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit)) {
            return false;
        }

        Transform current = hit.transform;

        while (current != null) {
            if (jelly.TryGetValue(current, out piece)) {
                return piece != null;
            }

            current = current.parent;
        }

        JellyCube hitJelly = hit.transform.GetComponentInParent<JellyCube>();

        if (hitJelly != null && jelly.TryGetValue(hitJelly.transform, out piece)) {
            return piece != null;
        }

        return false;
    }

    bool TryGetMouseWorldPosition(out Vector3 worldPosition) {
        worldPosition = transform.position;

        if (mainCamera == null || Mouse.current == null) {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane dragPlane = new Plane(Vector3.forward, Vector3.zero);

        if (!dragPlane.Raycast(ray, out float enter)) {
            return false;
        }

        worldPosition = ray.GetPoint(enter);
        return true;
    }

    void PlaceOnGridOrReturn() {
        if (activeJelly == null) {
            return;
        }

        JellyPiece placingJelly = activeJelly;
        bool wasHandled = false;
        PlacementRequested?.Invoke(placingJelly, GetDragRoot().position, OnPlacementCompleted);

        if (!wasHandled) {
            GetDragRoot().position = dragStartPosition;
        }

        void OnPlacementCompleted(bool placed) {
            wasHandled = true;

            if (placed) {
                if (activeJelly == placingJelly) {
                    activeJelly = null;
                }
            } else {
                GetDragRoot().position = dragStartPosition;
            }
        }
    }

    Transform GetDragRoot() {
        return activeJelly != null ? activeJelly.transform : transform;
    }
}
