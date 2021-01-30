using UnityEngine;

public class LocalPlayerController : PlayerController {
    public override bool isLocal => true;

    void FixedUpdate() {
    }
}
