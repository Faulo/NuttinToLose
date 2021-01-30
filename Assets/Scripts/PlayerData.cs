using System;
using UnityEngine;

[Serializable]
public class PlayerData {
    public string id;

    public float horizontalSpeed;
    public Vector3 position;
    public float angle;
    public int state;
    public bool isGrounded;

    public bool isJumping => playerState == PlayerState.Jumping;
    public bool isGliding => playerState == PlayerState.Gliding;
    public bool isFalling => playerState == PlayerState.Falling;
    public bool isDiggingUp => playerState == PlayerState.DiggingUp;
    public bool isFakeDigging => playerState == PlayerState.FakeDigging;
    public bool isRealDigging => playerState == PlayerState.RealDigging;

    public PlayerState playerState {
        get => (PlayerState)state;
        set => state = (int)value;
    }

    public bool isDigging => isRealDigging || isFakeDigging || isDiggingUp;
    public bool isAirborne => isJumping || isGliding || !isFalling;
    public Quaternion rotation => Quaternion.Euler(0, angle, 0);
}