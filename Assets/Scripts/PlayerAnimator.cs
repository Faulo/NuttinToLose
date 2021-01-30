using UnityEngine;

public class PlayerAnimator : MonoBehaviour {
    enum Parameters {
        horizontalSpeed,
        state,
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
        animator.SetInteger(nameof(Parameters.state), player.data.state);
    }
}
