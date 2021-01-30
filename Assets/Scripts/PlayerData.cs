using System;
using UnityEngine;

[Serializable]
public class PlayerData {
    public string id;

    public float horizontalSpeed;
    public Vector3 position;
    public float angle;

    public bool isJumping;
    public bool isGliding;
    public bool isGrounded;
    public bool isDiggingUp;
    public bool isFakeDigging;
    public bool isRealDigging;

    public bool isDigging => isRealDigging || isFakeDigging || isDiggingUp;
    public Quaternion rotation => Quaternion.Euler(0, angle, 0);
}
