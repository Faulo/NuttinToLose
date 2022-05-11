using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Slothsoft.UnityExtensions;
using UnityEngine;
using UnityEngine.Networking;

namespace NuttinToLose {
    public class ServerSentEventClient : MonoBehaviour {
        public enum RequestAPI {
            MicrosoftDotNet,
            UnityNetworking,
            SlothsoftJavaScript,
        }
        class PollState {
            public HttpWebRequest request;
            public bool isDone;
            public Queue<ServerSentEvent> queue;
        }
        class PushState {
            public HttpWebRequest request;
            public bool isDone;
        }

        public event Action<ServerSentEvent> onMessage;

        public string playerName => settings.playerName;

        [Header("Debug")]
        [SerializeField, Expandable]
        ServerSentEventSettings settings = default;
        [SerializeField]
        ServerSentEvent lastEvent = new();

        HttpWebRequest request;
        Queue<ServerSentEvent> queue = new();
        Coroutine poll;

        protected void FixedUpdate() {
            while (queue.Count > 0) {
                lastEvent = queue.Dequeue();
                onMessage?.Invoke(lastEvent);
            }
            if (poll is null) {
                poll = StartCoroutine(PollRoutine());
            }
        }
        public IEnumerator PushRoutine(string type, string data) {
            if (settings.isOffline) {
                yield break;
            }
            string uri = settings.pushUrl + $"&room={Uri.EscapeDataString(settings.roomName)}&type={Uri.EscapeDataString(type)}";
            switch (settings.api) {
                case RequestAPI.MicrosoftDotNet: {
                    static void callback(IAsyncResult result) {
                        var state = result.AsyncState as PushState;
                        var response = state.request.GetResponse();
                        state.isDone = true;
                    }
                    var request = WebRequest.CreateHttp(uri);
                    request.ContentType = "application/json";
                    request.Method = "POST";
                    using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                        streamWriter.Write(data);
                    }
                    var state = new PushState { request = request, isDone = false };
                    request.BeginGetResponse(callback, state);
                    yield return new WaitUntil(() => state.isDone);
                    break;
                }
                case RequestAPI.UnityNetworking:
                case RequestAPI.SlothsoftJavaScript: {
                    var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(data));
                    var request = new UnityWebRequest(uri, "POST", null, uploadHandler);
                    yield return request.SendWebRequest();
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
        }

        IEnumerator PollRoutine() {
            if (settings.isOffline) {
                yield break;
            }
            string uri = settings.pullUrl + $"&room={Uri.EscapeDataString(settings.roomName)}&lastId={lastEvent.id}";
            switch (settings.api) {
                case RequestAPI.MicrosoftDotNet: {
                    static void callback(IAsyncResult result) {
                        var state = result.AsyncState as PollState;
                        var stream = state.request.GetResponse().GetResponseStream();
                        using (var reader = new StreamReader(stream)) {
                            var lines = new List<string>();
                            while (!reader.EndOfStream) {
                                string line = reader.ReadLine();
                                //Debug.Log(line);
                                if (string.IsNullOrWhiteSpace(line)) {
                                    if (lines.Count > 0) {
                                        //Debug.Log($"Parsing:\n{string.Join("\n", lines)})");
                                        if (TryParseEvent(lines, out var eve)) {
                                            state.queue.Enqueue(eve);
                                        }
                                        //Debug.Log("Done Parsing");
                                        lines.Clear();
                                    }
                                } else {
                                    lines.Add(line);
                                }
                            }
                        }
                        state.isDone = true;
                    }
                    request = WebRequest.CreateHttp(uri);
                    request.AllowWriteStreamBuffering = false;
                    request.AllowReadStreamBuffering = false;
                    var state = new PollState { request = request, queue = queue };
                    request.BeginGetResponse(callback, state);
                    yield return new WaitUntil(() => state.isDone);
                    break;
                }
                case RequestAPI.UnityNetworking: {
                    var downloadHandler = new DownloadHandler(queue);
                    var request = new UnityWebRequest(uri, "GET", downloadHandler, null);
                    request.SetRequestHeader("Accept", "text/plain");
                    yield return request.SendWebRequest();
                    break;
                }
                case RequestAPI.SlothsoftJavaScript: {
                    // https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
                    // https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events
                    // Debug.Log("TODO: Implement Server-Sent Events via custom JavaScript EventSource");
#if PLATFORM_WEBGL
                    StartPolling(uri);
                    yield return new WaitUntil(() => false);
#else
                    Debug.LogError($"{nameof(RequestAPI.SlothsoftJavaScript)} is only available in WebGL builds!");
#endif
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
            Debug.Log("Lost connection to server! Reconnecting...");
            poll = null;
        }
#if PLATFORM_WEBGL
        [DllImport("__Internal")]
        static extern void StartPolling(string uri);

        protected void OnServerSentEvent(string message) {
            if (string.IsNullOrEmpty(message)) {
                return;
            }
            var obj = JsonUtility.FromJson<ServerSentEvent>(message);
            queue.Enqueue(obj);
        }
#endif

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
                    if (TryParseEvent(lines, out var eve)) {
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

        static bool TryParseEvent(IEnumerable<string> lines, out ServerSentEvent eve) {
            eve = new ServerSentEvent();
            bool hasData = false;
            foreach (string line in lines) {
                if (line.StartsWith("id:")) {
                    eve.id = int.Parse(line["id:".Length..].Trim());
                    hasData = true;
                }
                if (line.StartsWith("event:")) {
                    eve.type = line["event:".Length..].Trim();
                    hasData = true;
                }
                if (line.StartsWith("data:")) {
                    eve.data = line["data:".Length..].Trim();
                    hasData = true;
                }
            }

            return hasData;
        }
    }
}