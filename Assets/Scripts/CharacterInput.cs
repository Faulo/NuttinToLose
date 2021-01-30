using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour {
    [SerializeField]
    LocalPlayerController player = default;
    [SerializeField]
    CinemachineAxisInput axis = default;
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
    float rotationVelocity;

    [SerializeField, Range(0, 1)]
    float accelerationDuration = 0;
    [SerializeField, Range(0, 1)]
    float rotationDuration = 0;
    [SerializeField, Range(0, 100)]
    public float maximumSpeed = 10;
    [SerializeField, Range(0, 100)]
    float jumpStartSpeed = 10;
    [Header("Camera")]
    [SerializeField]
    bool invertX = false;
    [SerializeField]
    bool invertY = false;

    void OnEnable() {
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
    }
    void OnDisable() {
        moveAction.Disable();
        lookAction.Disable();
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
        float currentRotation = player.attachedRigidbody.rotation.eulerAngles.y;

        var direction = new Vector3(intendedMove.x, 0, intendedMove.y);
        direction = referenceCamera.transform.rotation * direction;
        direction *= maximumSpeed;

        var targetVelocity = new Vector3(direction.x, currentVelocity.y, direction.z);

        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref acceleration, accelerationDuration);
        if (intendsJump) {
            currentVelocity.y = jumpStartSpeed;
        }

        player.data.position = position;
        player.data.velocity = currentVelocity;
        if (direction != Vector3.zero) {
            float targetRotation = Quaternion.LookRotation(direction, Vector3.up).eulerAngles.y;
            currentRotation = Mathf.SmoothDampAngle(currentRotation, targetRotation, ref rotationVelocity, rotationDuration);
        }

        axis.input.x = invertX
            ? -intendedLook.x
            : intendedLook.x;
        axis.input.y = invertY
            ? -intendedLook.y
            : intendedLook.y;

        player.data.rotation = Quaternion.Euler(0, currentRotation, 0);
        player.UpdateState();
    }

    void UpdateIntentions() {
        intendedMove = moveAction.ReadValue<Vector2>();
        intendedLook = lookAction.ReadValue<Vector2>();
        intendsJump = jumpAction.phase == InputActionPhase.Started;
    }
}
