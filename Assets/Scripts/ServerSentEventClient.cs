using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

public class ServerSentEventClient : MonoBehaviour {
    class RequestState {
        public HttpWebRequest request;
        public Queue<ServerSentEvent> queue;
    }
    const int BUFFER_SIZE = 2048;

    public event Action<ServerSentEvent> onMessage;

    [Header("Debug")]
    [SerializeField]
    string pushUrl = "http://slothsoft.net/getData.php/sse/server.vct?mode=push&name=webrtc-YourNutz";
    [SerializeField]
    string pullUrl = "http://slothsoft.net/getData.php/sse/server.vct?mode=pull&name=webrtc-YourNutz&lastId=";
    [SerializeField]
    ServerSentEvent lastEvent = new ServerSentEvent();

    HttpWebRequest request;
    Queue<ServerSentEvent> queue = new Queue<ServerSentEvent>();


    void Start() {
    }

    void FixedUpdate() {
        while (queue.Count > 0) {
            lastEvent = queue.Dequeue();
            onMessage?.Invoke(lastEvent);
        }
        if (request is null) {
            ConnectPoll();
        }
    }

    public void PushEvent(string type, string data) {
        var request = WebRequest.CreateHttp(pushUrl + $"&type={Uri.EscapeDataString(type)}&data={Uri.EscapeDataString(data)}");
        request.GetResponse();
    }
    public void PushEvent(string type, string data, Action callback) {
        var request = WebRequest.CreateHttp(pushUrl + $"&type={Uri.EscapeDataString(type)}&data={Uri.EscapeDataString(data)}");
        request.BeginGetResponse(_ => callback(), null);
    }

    void ConnectPoll() {
        request = WebRequest.CreateHttp(pullUrl + lastEvent.id);
        request.AllowWriteStreamBuffering = false;
        request.AllowReadStreamBuffering = false;
        request.BeginGetResponse(PollCallback, new RequestState { request = request, queue = queue });
    }

    static void PollCallback(IAsyncResult result) {
        var state = result.AsyncState as RequestState;
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
    }

    static bool TryParseEvent(List<string> lines, out ServerSentEvent eve) {
        eve = new ServerSentEvent();
        bool hasData = false;
        for (int i = 0; i < lines.Count; i++) {
            if (lines[i].StartsWith("id:")) {
                eve.id = int.Parse(lines[i].Substring("id:".Length).Trim());
                hasData = true;
            }
            if (lines[i].StartsWith("event:")) {
                eve.type = lines[i].Substring("event:".Length).Trim();
                hasData = true;
            }
            if (lines[i].StartsWith("data:")) {
                eve.data = lines[i].Substring("data:".Length).Trim();
                hasData = true;
            }
        }

        return hasData;
    }
}