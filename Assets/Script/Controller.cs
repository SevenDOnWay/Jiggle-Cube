using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour {
    
    [SerializeField] InputActionAsset mouseInputSystem;

    [SerializeField] float speed;

    InputAction onPress;

    Camera mainCamera;
    bool pressedOnThisCube;


    private void OnEnable() {
        mainCamera = Camera.main;

        InputActionMap gameplayMap = mouseInputSystem.FindActionMap("Gameplay", true);
        onPress = gameplayMap.FindAction("Move Object", true);
        gameplayMap.Enable();
    }

    private void OnDisable() {
        mouseInputSystem.FindActionMap("Gameplay")?.Disable();
    }


    void Update() {
        if(onPress.WasPressedThisFrame()) {
            pressedOnThisCube = IsMouseOverThisCube();
        }

        if(onPress.WasReleasedThisFrame()) {
            pressedOnThisCube = false;
        }

        if(pressedOnThisCube && onPress.IsPressed()) {
            Vector2 mouseDir = Mouse.current.delta.ReadValue();
            gameObject.transform.position += (Vector3)mouseDir * speed * Time.deltaTime;
        }
    }

    bool IsMouseOverThisCube() {
        if(mainCamera == null || Mouse.current == null) {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out RaycastHit hit) && hit.transform == gameObject.transform;
    }
    
}
