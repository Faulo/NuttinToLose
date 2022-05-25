using UnityEngine;

namespace NuttinToLose.Networking {
    public class WebRTCAudioListener : MonoBehaviour {
        [SerializeField]
        bool useAudioListener = true;
        [SerializeField]
        AudioClip mic = default;

        float[] buffer = new float[512];
        protected void OnAudioFilterRead(float[] data, int channels) {
            if (useAudioListener) {
                //Audio.Update(data, data.Length);
            }
        }
        protected void Start() {
            if (!useAudioListener) {
                mic = Microphone.Start(Microphone.devices[0], true, 10, AudioSettings.outputSampleRate);
            }
        }
        protected void Update() {
            if (!useAudioListener) {
                mic.GetData(buffer, 0);
            }
        }
    }
}