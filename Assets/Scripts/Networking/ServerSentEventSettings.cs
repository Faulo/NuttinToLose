using System.Text.RegularExpressions;
using UnityEngine;

namespace NuttinToLose.Networking {
    [CreateAssetMenu]
    public class ServerSentEventSettings : ScriptableObject {
        enum RequestAPI {
            MicrosoftDotNet,
            UnityNetworking,
            SlothsoftJavaScript,
        }
#if PLATFORM_WEBGL && !UNITY_EDITOR
        RequestAPI api => RequestAPI.SlothsoftJavaScript;
#else
        RequestAPI api => m_api;
        [SerializeField]
        RequestAPI m_api = RequestAPI.MicrosoftDotNet;
#endif
        public IServerSentEventConnection apiConnection => api switch {
            RequestAPI.MicrosoftDotNet => SSEMicrosoftDotNet.instance,
            RequestAPI.UnityNetworking => SSEUnityNetworking.instance,
            RequestAPI.SlothsoftJavaScript => SSESlothsoftJavaScript.instance,
            _ => throw new System.NotImplementedException(),
        };

        [SerializeField]
        public string pushUrl = "http://slothsoft.net/getData.php/sse/server.vct?mode=push";
        [SerializeField]
        public string pullUrl = "http://slothsoft.net/getData.php/sse/server.vct?mode=pull";
        [SerializeField]
        public string roomName = "YourNutz";
        [SerializeField]
        public string playerName = "Ah�rnchen";
        [SerializeField]
        public bool isOffline = false;

        protected void OnValidate() {
            roomName = Regex.Replace(roomName, "[^a-zA-Z����]+", "");
            if (roomName == "") {
                roomName = "YourNutz";
            }
            playerName = Regex.Replace(playerName, "[^a-zA-Z����]+", "");
            if (playerName == "") {
                playerName = "Ah�rnchen";
            }
        }

        public void SetRoomName(string name) {
            roomName = name;
            OnValidate();
        }

        public void SetPlayerName(string name) {
            playerName = name;
            OnValidate();
        }

        public void SetOffline(bool isOffline) {
            this.isOffline = isOffline;
        }
    }
}