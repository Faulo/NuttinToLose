using Slothsoft.UnityExtensions;
using UnityEngine;

namespace NuttinToLose {
    public class StateMachineParticles : StateMachineBehaviour {
        [SerializeField, Expandable]
        ParticleSystem particlePrefab = default;
        [SerializeField]
        bool stopOnExit = true;

        ParticleSystem particleInstance = default;

        void Init(Animator animator) {
            if (!particleInstance) {
                particleInstance = Instantiate(particlePrefab, animator.transform);
            }
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Init(animator);
            particleInstance.Play();
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (stopOnExit) {
                particleInstance.Stop();
            }
        }
    }
}