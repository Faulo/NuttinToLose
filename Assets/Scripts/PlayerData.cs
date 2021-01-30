using System;
using UnityEngine;

[Serializable]
public class PlayerData {
    public string id;

    public Vector3 position = Vector3.zero;
    public Quaternion rotation = Quaternion.identity;
    public Vector3 velocity = Vector3.zero;

    public bool isJumping = false;
    public bool isGliding = false;
    public bool isGrounded = false;

    public float verticalSpeed => velocity.y;
    public float horizontalSpeed => new Vector2(velocity.x, velocity.z).magnitude;
}
