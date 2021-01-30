using UnityEngine;

public class PlayerController : MonoBehaviour {
    [SerializeField]
    public Rigidbody attachedRigidbody = default;
    [SerializeField]
    public bool isLocal = false;
    [SerializeField]
    public PlayerData data = new PlayerData();

    [Header("Remote Settings")]
    [SerializeField, Range(0, 1)]
    float interpolationDuration = 0;
    Vector3 positionVelocity;
    float rotationVelocity;

    void Awake() {
        OnValidate();
    }
    void OnValidate() {
        if (!attachedRigidbody) {
            attachedRigidbody = GetComponentInParent<Rigidbody>();
        }
    }

    void FixedUpdate() {
        if (isLocal) {
            UpdateStorage();
        } else {
            UpdateState();
        }
    }

    void UpdateState() {
        // position
        var currentPosition = transform.position;
        var targetPosition = data.position;
        currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition, ref positionVelocity, interpolationDuration);
        transform.position = currentPosition;

        // rotation
        float currentAngle = transform.rotation.eulerAngles.y;
        float targetAngle = data.angle;
        currentAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref rotationVelocity, interpolationDuration);
        transform.rotation = Quaternion.Euler(0, currentAngle, 0);
    }
    void UpdateStorage() {
        data.position = attachedRigidbody.position;
        data.angle = attachedRigidbody.rotation.eulerAngles.y;
        data.horizontalSpeed = new Vector2(attachedRigidbody.velocity.x, attachedRigidbody.velocity.z).magnitude;
    }
}
