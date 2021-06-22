using UnityEngine;

namespace NuttinToLose {
    public class PlayerAnimator : MonoBehaviour {
        enum Parameters {
            state,
            speed,
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
            animator.SetFloat(nameof(Parameters.speed), player.data.speed);
            animator.SetInteger(nameof(Parameters.state), player.data.state);
        }
    }
}