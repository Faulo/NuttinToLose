using UnityEngine;

namespace NuttinToLose {
    public class StateMachineDigging : StateMachineBehaviour {
        [SerializeField]
        GameObjectEvent onFindNut = new();
        [SerializeField]
        GameObjectEvent onAbort = new();

        bool foundNut;
#pragma warning disable UNT0021 // Prefer protected Unity Message.
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            foundNut = false;
            var player = animator.GetComponentInParent<PlayerController>();
            player.onNutChange += count => foundNut = true;
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (foundNut) {
                onFindNut.Invoke(animator.gameObject);
            } else {
                onAbort.Invoke(animator.gameObject);
            }
        }
#pragma warning restore UNT0021 // Prefer protected Unity Message.
    }
}