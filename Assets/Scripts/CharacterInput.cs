using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour {
    [SerializeField]
    LocalPlayerController player = default;
    [SerializeField]
    Camera referenceCamera = default;
    [SerializeField]
    InputAction moveAction = new InputAction();
    [SerializeField]
    InputAction lookAction = new InputAction();
    [SerializeField]
    InputAction jumpAction = new InputAction();

    bool isJumping = false;

    Vector2 intendedMove;
    Vector2 intendedLook;
    bool intendsJump;

    Vector3 acceleration;
    [SerializeField, Range(0, 1)]
    float accelerationDuration = 0;
    [SerializeField, Range(0, 100)]
    public float maximumSpeed = 10;
    [SerializeField, Range(0, 100)]
    float jumpStartSpeed = 10;

    void OnEnable() {
        moveAction.Enable();
        jumpAction.Enable();
    }
    void OnDisable() {
        moveAction.Disable();
        jumpAction.Disable();
    }
    void Awake() {
        OnValidate();
    }
    void OnValidate() {
        if (!player) {
            player = GetComponentInParent<LocalPlayerController>();
        }
        if (!referenceCamera) {
            referenceCamera = FindObjectOfType<Camera>();
        }
    }

    void FixedUpdate() {
        UpdateIntentions();

        var position = player.attachedRigidbody.position;
        var currentVelocity = player.attachedRigidbody.velocity;
        var currentRotation = player.attachedRigidbody.rotation;

        var direction = new Vector3(intendedMove.x, 0, intendedMove.y);
        //direction = referenceCamera.transform.rotation * direction;
        direction *= maximumSpeed;

        var targetVelocity = new Vector3(direction.x, currentVelocity.y, direction.z);

        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref acceleration, accelerationDuration);
        if (intendsJump) {
            currentVelocity.y = jumpStartSpeed;
        }

        player.data.position = position;
        player.data.velocity = currentVelocity;
        if (direction == Vector3.zero) {
            player.data.rotation = currentRotation;
        } else {
            player.data.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
        player.UpdateState();
    }

    void UpdateIntentions() {
        intendedMove = moveAction.ReadValue<Vector2>();
        intendedLook = lookAction.ReadValue<Vector2>();
        intendsJump = jumpAction.phase == InputActionPhase.Started;
    }
}
