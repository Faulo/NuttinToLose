using System.Text.RegularExpressions;
using UnityEngine;

namespace NuttinToLose {
    [CreateAssetMenu]
    public class ServerSentEventSettings : ScriptableObject {
#if PLATFORM_WEBGL && !UNITY_EDITOR
        public ServerSentEventClient.RequestAPI api => ServerSentEventClient.RequestAPI.SlothsoftJavaScript;
#else
        public ServerSentEventClient.RequestAPI api => m_api;
        [SerializeField]
        ServerSentEventClient.RequestAPI m_api = ServerSentEventClient.RequestAPI.MicrosoftDotNet;
#endif
        [SerializeField]
        public string pushUrl = "http://slothsoft.net/getData.php/sse/server.vct?mode=push";
        [SerializeField]
        public string pullUrl = "http://slothsoft.net/getData.php/sse/server.vct?mode=pull";
        [SerializeField]
        public string roomName = "YourNutz";
        [SerializeField]
        public string playerName = "Ahörnchen";
        [SerializeField]
        public bool isOffline = false;

        protected void OnValidate() {
            roomName = Regex.Replace(roomName, "[^a-zA-Zöäüß]+", "");
            if (roomName == "") {
                roomName = "YourNutz";
            }
            playerName = Regex.Replace(playerName, "[^a-zA-Zöäüß]+", "");
            if (playerName == "") {
                playerName = "Ahörnchen";
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