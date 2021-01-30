using UnityEngine;

public class PlayerController : MonoBehaviour {
    [SerializeField]
    public Rigidbody attachedRigidbody = default;
    [SerializeField]
    public bool isLocal = false;
    [SerializeField]
    public PlayerData data = new PlayerData();

    void Awake() {
        OnValidate();
    }
    void OnValidate() {
        if (!attachedRigidbody) {
            attachedRigidbody = GetComponentInParent<Rigidbody>();
        }
    }

    void FixedUpdate() {
        if (!isLocal) {
            UpdateStorage();
        }
    }

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
