using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NuttinToLose.Networking {
    public class SSEUnityNetworking : IServerSentEventConnection {
        public static readonly SSEUnityNetworking instance = new();

        class DownloadHandler : DownloadHandlerScript {
            const int BUFFER_SIZE = 10240;
            readonly Queue<ServerSentEvent> queue;
            readonly StringBuilder builder = new();
            public DownloadHandler(Queue<ServerSentEvent> queue) : base(new byte[BUFFER_SIZE]) {
                this.queue = queue;
            }
            protected override bool ReceiveData(byte[] data, int dataLength) {
                string text = Encoding.UTF8.GetString(data, 0, dataLength);
                builder.Append(text);
                text = builder.ToString();
                bool didSomething = false;
                for (int i = text.IndexOf("\n\n"); i != -1; i = text.IndexOf("\n\n")) {
                    didSomething = true;
                    string[] lines = text.Substring(0, i).Split('\n');
                    text = text[(i + 2)..];
                    if (ServerSentEventClient.TryParseEvent(lines, out var eve)) {
                        queue.Enqueue(eve);
                    }
                }
                if (didSomething) {
                    builder.Clear();
                    builder.Append(text);
                }
                return Application.isPlaying;
            }
        }

        SSEUnityNetworking() {
        }

        public IEnumerator CreatePushRoutine(string uri, string data) {
            using var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(data));
            using var request = new UnityWebRequest(uri, "POST", null, uploadHandler);
            yield return request.SendWebRequest();
        }

        public IEnumerator CreatePollRoutine(string uri, Queue<ServerSentEvent> queue) {
            using var downloadHandler = new DownloadHandler(queue);
            using var request = new UnityWebRequest(uri, "GET", downloadHandler, null);
            request.SetRequestHeader("Accept", "text/plain");
            yield return request.SendWebRequest();
        }
    }
}