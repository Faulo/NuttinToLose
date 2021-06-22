using UnityEngine;

namespace NuttinToLose {
    public class StateMachineEvents : StateMachineBehaviour {
        [SerializeField]
        GameObjectEvent onStateEvent = new GameObjectEvent();
        [SerializeField]
        GameObjectEvent onStateExit = new GameObjectEvent();
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            onStateEvent.Invoke(animator.gameObject);
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            onStateExit.Invoke(animator.gameObject);
        }
    }
}