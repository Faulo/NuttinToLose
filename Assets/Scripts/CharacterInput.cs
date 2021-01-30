using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour {
    [SerializeField]
    LocalPlayerController controller = default;
    [SerializeField]
    InputAction moveAction = new InputAction();
    [SerializeField]
    InputAction jumpAction = new InputAction();

    void OnEnable() {
        moveAction.Enable();
        jumpAction.Enable();
    }
    void OnDisable() {
        moveAction.Disable();
        jumpAction.Disable();
    }

    void Update() {
        var input = moveAction.ReadValue<Vector2>();
        controller.currentInput.x = input.x;
        controller.currentInput.z = input.y;
    }
}
