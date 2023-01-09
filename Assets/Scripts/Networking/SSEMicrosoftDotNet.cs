using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

namespace NuttinToLose.Networking {
    public class SSEMicrosoftDotNet : IServerSentEventConnection {
        public static readonly SSEMicrosoftDotNet instance = new();

        class PollState {
            public HttpWebRequest request;
            public bool isDone;
            public Queue<ServerSentEvent> queue;
        }
        class PushState {
            public HttpWebRequest request;
            public bool isDone;
        }

        HttpWebRequest request;

        SSEMicrosoftDotNet() {
        }

        public IEnumerator CreatePushRoutine(string uri, string data) {
            var request = WebRequest.CreateHttp(uri);
            request.ContentType = "application/json";
            request.Method = "POST";
            using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                streamWriter.Write(data);
            }
            var state = new PushState { request = request, isDone = false };
            request.BeginGetResponse(HandlePushResponse, state);
            yield return new WaitUntil(() => state.isDone);
        }

        static void HandlePushResponse(IAsyncResult result) {
            var state = result.AsyncState as PushState;
            var response = state.request.GetResponse();
            state.isDone = true;
        }

        public IEnumerator CreatePollRoutine(string uri, Queue<ServerSentEvent> queue) {
            request = WebRequest.CreateHttp(uri);
            request.AllowWriteStreamBuffering = false;
            request.AllowReadStreamBuffering = false;
            var state = new PollState { request = request, queue = queue };
            request.BeginGetResponse(HandlePollResponse, state);
            yield return new WaitUntil(() => state.isDone);
        }

        static void HandlePollResponse(IAsyncResult result) {
            var state = result.AsyncState as PollState;
            using (var stream = state.request.GetResponse().GetResponseStream()) {
                using var reader = new StreamReader(stream);
                var lines = new List<string>();
                while (!reader.EndOfStream) {
                    string line = reader.ReadLine();
                    //Debug.Log(line);
                    if (string.IsNullOrWhiteSpace(line)) {
                        if (lines.Count > 0) {
                            //Debug.Log($"Parsing:\n{string.Join("\n", lines)})");
                            if (ServerSentEventClient.TryParseEvent(lines, out var eve)) {
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
    }
}