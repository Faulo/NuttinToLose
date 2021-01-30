using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour {
    enum Parameters {
        horizontalSpeed,
        verticalSpeed,
        isGrounded,
        isJumping,
        isGliding
    }
    [SerializeField]
    PlayerController player = default;
    [SerializeField]
    Animator animator = default;

    void Awake() {
        OnValidate();
    }
    void OnValidate() {
        if (!player) {
            player = GetComponentInParent<PlayerController>();
        }
        if (!animator) {
            animator = GetComponent<Animator>();
        }
    }
    void Update() {
        animator.SetFloat(nameof(Parameters.horizontalSpeed), player.data.horizontalSpeed);
        animator.SetFloat(nameof(Parameters.verticalSpeed), player.data.verticalSpeed);
        animator.SetBool(nameof(Parameters.isGrounded), player.data.isGrounded);
        animator.SetBool(nameof(Parameters.isJumping), player.data.isJumping);
        animator.SetBool(nameof(Parameters.isGliding), player.data.isGliding);
    }
}
