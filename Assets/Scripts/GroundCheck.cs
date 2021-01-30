using UnityEngine;

public class GroundCheck : MonoBehaviour {
    public bool isGrounded;
    bool hasTriggered;
    void OnTriggerStay(Collider other) {
        hasTriggered = true;
        isGrounded = true;
    }
    void FixedUpdate() {
        if (hasTriggered) {
            hasTriggered = false;
        } else {
            isGrounded = false;
        }
    }
}
