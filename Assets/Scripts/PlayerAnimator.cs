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

        protected void Awake() {
            OnValidate();
        }
        protected void OnValidate() {
            if (!player) {
                player = GetComponentInParent<PlayerController>();
            }
            if (!animator) {
                animator = GetComponent<Animator>();
            }
        }
        protected void Update() {
            animator.SetFloat(nameof(Parameters.speed), player.data.speed);
            animator.SetInteger(nameof(Parameters.state), player.data.state);
        }
    }
}