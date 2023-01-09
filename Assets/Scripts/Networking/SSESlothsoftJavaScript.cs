using System.Collections;
using System.Collections.Generic;
#if PLATFORM_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine;
#endif

namespace NuttinToLose.Networking {
    public class SSESlothsoftJavaScript : IServerSentEventConnection {
        public static readonly SSESlothsoftJavaScript instance = new();

        SSESlothsoftJavaScript() {
        }

        public IEnumerator CreatePushRoutine(string uri, string data) {
            yield return SSEUnityNetworking.instance.CreatePushRoutine(uri, data);
        }

#if PLATFORM_WEBGL && !UNITY_EDITOR
        public IEnumerator CreatePollRoutine(string uri, Queue<ServerSentEvent> queue) {
            StartPolling(uri);
            yield return new WaitWhile(() => Application.isPlaying);
        }

        [DllImport("__Internal")]
        static extern void StartPolling(string uri);
#else
        public IEnumerator CreatePollRoutine(string uri, Queue<ServerSentEvent> queue) {
            yield return SSEUnityNetworking.instance.CreatePollRoutine(uri, queue);
        }
#endif
    }
}