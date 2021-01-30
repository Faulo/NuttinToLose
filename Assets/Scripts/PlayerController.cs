using System;
using UnityEngine;

public abstract class PlayerController : MonoBehaviour {
    [SerializeField]
    public Rigidbody attachedRigidbody = default;
    public abstract bool isLocal { get; }

    public PlayerData data = new PlayerData();

    public void UpdateState() {
        attachedRigidbody.position = data.position;
        attachedRigidbody.rotation = data.rotation;
        attachedRigidbody.velocity = data.velocity;
    }
    public void UpdateStorage() {
        data.position = attachedRigidbody.position;
        data.rotation = attachedRigidbody.rotation;
        data.velocity = attachedRigidbody.velocity;
    }
}
