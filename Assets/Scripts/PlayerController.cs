using System;
using UnityEngine;

public abstract class PlayerController : MonoBehaviour {
    [SerializeField]
    public Rigidbody attachedRigidbody = default;

    public abstract bool isLocal { get; }

    public PlayerData data = new PlayerData();

    public void SetData(PlayerData data) {
        this.data = data;
        attachedRigidbody.position = data.position;
        attachedRigidbody.rotation = data.rotation;
        attachedRigidbody.velocity = data.velocity;
    }
}
