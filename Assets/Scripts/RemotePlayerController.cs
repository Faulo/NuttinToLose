using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemotePlayerController : PlayerController {
    public override bool isLocal => false;

    void FixedUpdate() {
        UpdateStorage();
    }
}
