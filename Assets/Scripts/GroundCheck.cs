using UnityEngine;

public class GroundCheck : MonoBehaviour {
    [SerializeField]
    LayerMask groundLayers = default;

    public bool isGrounded;
    public DigSpot spot;

    bool foundGround;
    bool foundSpot;

    void OnTriggerStay(Collider other) {
        int layer = 1 << other.gameObject.layer;
        if ((layer & groundLayers.value) == layer) {
            foundGround = true;
            isGrounded = true;
        }
        if (other.TryGetComponent<DigSpot>(out var spot)) {
            foundSpot = true;
            this.spot = spot;
        }
    }

    void FixedUpdate() {
        if (foundGround) {
            foundGround = false;
        } else {
            isGrounded = false;
        }
        if (foundSpot) {
            foundSpot = false;
        } else {
            spot = null;
        }
    }
}
