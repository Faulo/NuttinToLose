using Slothsoft.UnityExtensions;
using UnityEngine;

namespace NuttinToLose {
    public class StateMachineAudio : StateMachineBehaviour {
        [SerializeField, Expandable]
        AudioSource audioPrefab = default;
        [SerializeField]
        bool stopOnExit = true;
        [SerializeField, Range(0, 10)]
        float offset = 0;
        [SerializeField, Range(0, 3)]
        float minimumPitch = 1;
        [SerializeField, Range(0, 3)]
        float maximumPitch = 1;
        [SerializeField, Range(0, 3)]
        float minimumVolume = 1;
        [SerializeField, Range(0, 3)]
        float maximumVolume = 1;

        AudioSource audioInstance = default;

        void Init(Animator animator) {
            if (!audioInstance) {
                audioInstance = Instantiate(audioPrefab, animator.transform);
            }
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Init(animator);
            audioInstance.time = offset;
            audioInstance.pitch = Random.Range(minimumPitch, maximumPitch);
            audioInstance.volume = Random.Range(minimumVolume, maximumVolume);
            audioInstance.Play();
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (stopOnExit) {
                audioInstance.Stop();
            }
        }
    }
}