using System;
using UnityEngine;
using UnityEngine.UI;

namespace NuttinToLose {
    public class ServerDebug : MonoBehaviour {
        class Message {
            public string text;
        }
        [SerializeField]
        ServerSentEventClient client = default;
        [SerializeField]
        InputField messageInput = default;
        [SerializeField]
        InputField messageOutput = default;
        [SerializeField]
        Button sendButton = default;

        protected void Start() {
            client.onMessage += HandleMessage;
            sendButton.onClick.AddListener(PushMessage);
        }
        void HandleMessage(ServerSentEvent eve) {
            Debug.Log(eve.type);
            switch (eve.type) {
                case "message":
                    try {
                        var message = JsonUtility.FromJson<Message>(eve.data);
                        messageOutput.text = message.text;
                    } catch (Exception e) {
                        Debug.Log($"Error parsing {eve}: {e}");
                    }
                    break;
            }
        }
        void PushMessage() {
            StartCoroutine(client.PushRoutine("message", JsonUtility.ToJson(new Message { text = messageInput.text })));
        }
    }
}