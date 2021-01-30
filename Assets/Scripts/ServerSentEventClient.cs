using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

public class ServerSentEventClient : MonoBehaviour {
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

    [Header("Debug")]
    [SerializeField]
    string pushUrl = "http://slothsoft.net/getData.php/sse/server.vct?mode=push&name=webrtc-YourNutz";
    [SerializeField]
    string pullUrl = "http://slothsoft.net/getData.php/sse/server.vct?mode=pull&name=webrtc-YourNutz&lastId=";
    [SerializeField]
    ServerSentEvent lastEvent = new ServerSentEvent();

    HttpWebRequest request;
    Queue<ServerSentEvent> queue = new Queue<ServerSentEvent>();
    Coroutine poll;

    void FixedUpdate() {
        while (queue.Count > 0) {
            lastEvent = queue.Dequeue();
            onMessage?.Invoke(lastEvent);
        }
        if (poll is null) {
            poll = StartCoroutine(PollRoutine());
        }
    }

    public IEnumerator PushRoutine(string type, string data) {
        string uri = pushUrl + $"&type={Uri.EscapeDataString(type)}";
        //var request = WebRequest.CreateHttp(pushUrl + $"&type={Uri.EscapeDataString(type)}&data={Uri.EscapeDataString(data)}");
        var request = WebRequest.CreateHttp(uri);
        request.ContentType = "application/json";
        request.Method = "POST";
        using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
            streamWriter.Write(data);
        }
        var state = new PushState { request = request, isDone = false };
        request.BeginGetResponse(PushCallback, state);
        yield return new WaitUntil(() => state.isDone);
    }
    static void PushCallback(IAsyncResult result) {
        var state = result.AsyncState as PushState;
        var response = state.request.GetResponse();
        state.isDone = true;
    }

    IEnumerator PollRoutine() {
        request = WebRequest.CreateHttp(pullUrl + lastEvent.id);
        request.AllowWriteStreamBuffering = false;
        request.AllowReadStreamBuffering = false;
        var state = new PollState { request = request, queue = queue };
        request.BeginGetResponse(PollCallback, state);
        yield return new WaitUntil(() => state.isDone);
        Debug.Log("Lost connection to server! Reconnecting...");
        yield return new WaitForSeconds(1);
        poll = null;
    }

    static void PollCallback(IAsyncResult result) {
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