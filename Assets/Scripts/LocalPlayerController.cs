using UnityEngine;

public class LocalPlayerController : PlayerController {
    [SerializeField, Range(0, 100)]
    public float maximumSpeed = 10;

    public Vector3 currentInput;

    public override bool isLocal => true;

    void FixedUpdate() {
        var currentVelocity = attachedRigidbody.velocity;
        var intendedVelocity = currentInput * maximumSpeed;
        intendedVelocity.y = currentVelocity.y;
        attachedRigidbody.velocity = intendedVelocity;

        data.position = attachedRigidbody.position;
        data.rotation = attachedRigidbody.rotation;
        data.velocity = attachedRigidbody.velocity;
    }
}
