#if !PLATFORM_WEBGL
using Unity.WebRTC;
#endif
using UnityEngine;

namespace NuttinToLose.Networking {
    public class WebRTCAudioListener : MonoBehaviour {
#if !PLATFORM_WEBGL
        [SerializeField]
        bool useAudioListener = true;
        [SerializeField]
        AudioClip mic = default;

        float[] buffer = new float[512];
        void OnAudioFilterRead(float[] data, int channels) {
            if (useAudioListener) {
                Audio.Update(data, data.Length);
            }
        }
        void Start() {
            if (!useAudioListener) {
                mic = Microphone.Start(Microphone.devices[0], true, 10, AudioSettings.outputSampleRate);
            }
        }
        void Update() {
            if (!useAudioListener) {
                mic.GetData(buffer, 0);
            }
        }
#endif
    }
}