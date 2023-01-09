using UnityEngine;

namespace NuttinToLose {
    public class StateMachineEvents : StateMachineBehaviour {
        [SerializeField]
        GameObjectEvent onStateEvent = new();
        [SerializeField]
        GameObjectEvent onStateExit = new();

#pragma warning disable UNT0021 // Prefer protected Unity Message.
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            onStateEvent.Invoke(animator.gameObject);
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            onStateExit.Invoke(animator.gameObject);
        }
#pragma warning restore UNT0021 // Prefer protected Unity Message.
    }
}