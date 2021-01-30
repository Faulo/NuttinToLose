using Slothsoft.UnityExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour {
    [SerializeField]
    PlayerController player = default;
    [SerializeField]
    GroundCheck groundCheck = default;
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

    Vector2 intendedMove;
    Vector2 intendedLook;
    bool intendsJump;

    Vector3 acceleration;
    float rotationVelocity;

    [SerializeField, Expandable]
    PlayerSettings idleSettings = default;
    [SerializeField, Expandable]
    PlayerSettings digSettings = default;
    [SerializeField, Expandable]
    PlayerSettings jumpSettings = default;
    [SerializeField, Expandable]
    PlayerSettings fallSettings = default;
    [SerializeField, Expandable]
    PlayerSettings glideSettings = default;

    [SerializeField, Range(0, 100)]
    float jumpStopSpeed = 2;
    [SerializeField, Range(0, 1)]
    float rotationDeadzone = 0;

    PlayerSettings currentSettings => player.data.playerState switch {
        PlayerState.Idle => idleSettings,
        PlayerState.Jumping => jumpSettings,
        PlayerState.Gliding => glideSettings,
        PlayerState.Falling => fallSettings,
        PlayerState.RealDigging => digSettings,
        PlayerState.FakeDigging => digSettings,
        PlayerState.DiggingUp => digSettings,
        _ => throw new System.NotImplementedException(),
    };

    float accelerationDuration => currentSettings.accelerationDuration;
    float rotationDuration => currentSettings.rotationDuration;
    float maximumSpeed => currentSettings.maximumSpeed;
    public float forwardBoost => currentSettings.forwardBoost;
    public float upwardsBoost => currentSettings.upwardsBoost;

    [Header("Camera")]
    [SerializeField]
    bool invertX = false;
    [SerializeField]
    bool invertY = false;

    bool canStartJump;

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
            player = GetComponentInParent<PlayerController>();
        }
        if (!referenceCamera) {
            referenceCamera = FindObjectOfType<Camera>();
        }
    }

    void FixedUpdate() {
        UpdateIntentions();
        player.data.isGrounded = groundCheck.isGrounded;

        var position = player.attachedRigidbody.position;
        var currentVelocity = player.attachedRigidbody.velocity;
        float currentRotation = player.attachedRigidbody.rotation.eulerAngles.y;

        var direction = new Vector3(intendedMove.x, 0, intendedMove.y);
        direction = referenceCamera.transform.rotation * direction;
        direction *= maximumSpeed;

        var targetVelocity = new Vector3(direction.x, currentVelocity.y, direction.z);

        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref acceleration, accelerationDuration);

        ProcessJump(ref currentVelocity);

        currentVelocity += Physics.gravity * currentSettings.gravity * Time.deltaTime;

        player.attachedRigidbody.velocity = currentVelocity;
        if (direction.magnitude > rotationDeadzone) {
            float targetRotation = Quaternion.LookRotation(direction, Vector3.up).eulerAngles.y;
            currentRotation = Mathf.SmoothDampAngle(currentRotation, targetRotation, ref rotationVelocity, rotationDuration);
        }

        axis.input.x = invertX
            ? -intendedLook.x
            : intendedLook.x;
        axis.input.y = invertY
            ? -intendedLook.y
            : intendedLook.y;

        player.attachedRigidbody.rotation = Quaternion.Euler(0, currentRotation, 0);

        player.attachedRigidbody.drag = currentSettings.drag;
    }

    void ProcessJump(ref Vector3 velocity) {
        // we're jumping, so we might wanna stop
        if (player.data.playerState == PlayerState.Jumping) {
            if (!intendsJump) {
                player.data.playerState = PlayerState.Falling;
                velocity.y = jumpStopSpeed;
                return;
            }
            if (velocity.y < jumpStopSpeed) {
                player.data.playerState = PlayerState.Falling;
                return;
            }
            return;
        }

        //we're gliding, so we might wanna stop
        if (player.data.playerState == PlayerState.Gliding) {
            if (!intendsJump || player.data.isGrounded) {
                player.data.playerState = PlayerState.Falling;
                return;
            }
            return;
        }

        // we're grounded, so we might wanna jump
        if (player.data.isGrounded) {
            if (intendsJump && canStartJump) {
                player.data.playerState = PlayerState.Jumping;
                canStartJump = false;
                velocity += transform.forward * forwardBoost;
                velocity += Vector3.up * upwardsBoost;
                return;
            }
            player.data.playerState = PlayerState.Idle;
            return;
        }

        // we're falling, so we might wanna glide
        if (intendsJump && canStartJump) {
            player.data.playerState = PlayerState.Gliding;
            canStartJump = false;
            velocity += transform.forward * forwardBoost;
            velocity += Vector3.up * upwardsBoost;
            return;
        }
        player.data.playerState = PlayerState.Falling;
    }

    void UpdateIntentions() {
        intendedMove = moveAction.ReadValue<Vector2>();
        intendedLook = lookAction.ReadValue<Vector2>();
        intendsJump = jumpAction.phase == InputActionPhase.Started;
        if (!intendsJump) {
            canStartJump = true;
        }
    }
}
