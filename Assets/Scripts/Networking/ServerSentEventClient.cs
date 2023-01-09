using System;
using System.Collections;
using System.Collections.Generic;
using Slothsoft.UnityExtensions;
using UnityEngine;

namespace NuttinToLose.Networking {
    public class ServerSentEventClient : MonoBehaviour {
        public event Action<ServerSentEvent> onMessage;

        public string playerName => settings.playerName;

        [Header("Debug")]
        [SerializeField, Expandable]
        ServerSentEventSettings settings = default;
        [SerializeField]
        ServerSentEvent lastEvent = new();

        readonly Queue<ServerSentEvent> queue = new();
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

        public Coroutine PushMessage(string type, string message) {
            if (settings.isOffline) {
                return default;
            }
            string uri = settings.pushUrl + $"&room={Uri.EscapeDataString(settings.roomName)}&type={Uri.EscapeDataString(type)}";
            return StartCoroutine(settings.apiConnection.CreatePushRoutine(uri, message));
        }

        IEnumerator PollRoutine() {
            if (settings.isOffline) {
                yield break;
            }
            string uri = settings.pullUrl + $"&room={Uri.EscapeDataString(settings.roomName)}&lastId={lastEvent.id}";
            yield return settings.apiConnection.CreatePollRoutine(uri, queue);
            Debug.Log("Lost connection to server! Reconnecting...");
            poll = null;
        }

        protected void OnServerSentEvent(string message) {
            if (string.IsNullOrEmpty(message)) {
                return;
            }
            var obj = JsonUtility.FromJson<ServerSentEvent>(message);
            queue.Enqueue(obj);
        }

        public static bool TryParseEvent(IEnumerable<string> lines, out ServerSentEvent eve) {
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