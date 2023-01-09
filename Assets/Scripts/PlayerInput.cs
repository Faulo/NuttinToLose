using System;
using NuttinToLose.Networking;
using Slothsoft.UnityExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NuttinToLose {
    public class PlayerInput : MonoBehaviour {
        [SerializeField]
        PlayerController player = default;
        [SerializeField]
        ServerConnection server = default;
        [SerializeField]
        GroundCheck groundCheck = default;
        [SerializeField]
        CinemachineAxisInput axis = default;
        [SerializeField]
        Camera referenceCamera = default;
        [SerializeField]
        InputAction moveAction = new();
        [SerializeField]
        InputAction lookAction = new();
        [SerializeField]
        InputAction jumpAction = new();
        [SerializeField]
        InputAction realDigAction = new();
        [SerializeField]
        InputAction fakeDigAction = new();
        [SerializeField]
        InputAction digUpAction = new();

        Vector2 intendedMove;
        Vector2 intendedLook;
        bool intendsJump;
        bool intendsRealDig;
        bool intendsFakeDig;
        bool intendsDigUp;

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
        float forwardBoost => currentSettings.forwardBoost;
        float upwardsBoost => currentSettings.upwardsBoost;
        float digDuration => currentSettings.digDuration;
        bool isGrounded => groundCheck.isGrounded;

        [Header("Camera")]
        [SerializeField]
        bool invertX = false;
        [SerializeField]
        bool invertY = false;

        bool canStartJump;
        float digTimer;

        protected void OnEnable() {
            moveAction.Enable();
            lookAction.Enable();
            jumpAction.Enable();
            realDigAction.Enable();
            fakeDigAction.Enable();
            digUpAction.Enable();
            server.onStateEnter += HandleStateChange;
        }
        protected void OnDisable() {
            moveAction.Disable();
            lookAction.Disable();
            jumpAction.Disable();
            realDigAction.Disable();
            fakeDigAction.Disable();
            digUpAction.Disable();
            server.onStateEnter -= HandleStateChange;
        }
        void HandleStateChange(WorldState state) {
            switch (state) {
                case WorldState.Inactive:
                    break;
                case WorldState.Lobby:
                    break;
                case WorldState.Fall:
                    break;
                case WorldState.Winter:
                    break;
                case WorldState.HighScore:
                    enabled = false;
                    break;
                default:
                    throw new NotImplementedException(state.ToString());
            }
        }
        protected void Awake() {
            OnValidate();
        }
        protected void OnValidate() {
            if (!player) {
                player = GetComponentInParent<PlayerController>();
            }
            if (!server) {
                server = FindObjectOfType<ServerConnection>();
            }
            if (!referenceCamera) {
                referenceCamera = Camera.main;
            }
        }

        protected void FixedUpdate() {
            UpdateIntentions();

            var position = player.attachedRigidbody.position;
            var currentVelocity = player.attachedRigidbody.velocity;
            float currentRotation = player.attachedRigidbody.rotation.eulerAngles.y;

            var direction = new Vector3(intendedMove.x, 0, intendedMove.y);
            direction = referenceCamera.transform.rotation * direction;
            direction *= maximumSpeed;

            var targetVelocity = new Vector3(direction.x, currentVelocity.y, direction.z);

            currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref acceleration, accelerationDuration);

            ProcessJump(ref currentVelocity);

            currentVelocity += currentSettings.gravity * Time.deltaTime * Physics.gravity;

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
            // if we're digging, we gotta keep digging
            if (player.data.isDigging) {
                if (player.data.playerState == PlayerState.RealDigging && !groundCheck.spot) {
                    if (digTimer > 0) {
                        digTimer -= Time.deltaTime;
                        return;
                    } else {
                        player.RealDig();
                    }
                }
                if (player.data.playerState == PlayerState.FakeDigging && !groundCheck.spot) {
                    if (digTimer > 0) {
                        digTimer -= Time.deltaTime;
                        return;
                    } else {
                        player.FakeDig();
                    }
                }
                if (player.data.playerState == PlayerState.DiggingUp && groundCheck.spot) {
                    if (digTimer > 0) {
                        digTimer -= Time.deltaTime;
                        return;
                    } else {
                        player.DigUp(groundCheck.spot);
                    }
                }
                // we're done digging
                player.data.playerState = PlayerState.Idle;
            }

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
                if (!intendsJump || isGrounded) {
                    player.data.playerState = PlayerState.Falling;
                    return;
                }
                return;
            }

            // we're grounded, so we might wanna jump
            if (isGrounded) {
                if (intendsJump && canStartJump) {
                    player.data.playerState = PlayerState.Jumping;
                    canStartJump = false;
                    velocity += transform.forward * forwardBoost;
                    velocity += Vector3.up * upwardsBoost;
                    return;
                }
                // ... or dig!
                if (intendsRealDig && groundCheck.canDig && !groundCheck.spot && player.nutCount > 0) {
                    if (player.data.playerState == PlayerState.Idle) {
                        player.data.playerState = PlayerState.RealDigging;
                        digTimer = digDuration;
                        return;
                    }
                }
                if (intendsFakeDig && groundCheck.canDig && !groundCheck.spot) {
                    if (player.data.playerState == PlayerState.Idle) {
                        player.data.playerState = PlayerState.FakeDigging;
                        digTimer = digDuration;
                        return;
                    }
                }
                if (intendsDigUp && groundCheck.spot) {
                    if (player.data.playerState == PlayerState.Idle) {
                        player.data.playerState = PlayerState.DiggingUp;
                        digTimer = digDuration;
                        return;
                    }
                }

                // nothing more to do when grounded
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
            intendsRealDig = realDigAction.phase == InputActionPhase.Started;
            intendsFakeDig = fakeDigAction.phase == InputActionPhase.Started;
            intendsDigUp = digUpAction.phase == InputActionPhase.Started;
            if (!intendsJump) {
                canStartJump = true;
            }
        }
    }
}