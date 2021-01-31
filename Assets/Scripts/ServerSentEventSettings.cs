using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu]
public class ServerSentEventSettings : ScriptableObject
{
    [SerializeField]
    public ServerSentEventClient.RequestAPI api = ServerSentEventClient.RequestAPI.MicrosoftWebRequest;
    [SerializeField]
    public string pushUrl = "http://slothsoft.net/getData.php/sse/server.vct?mode=push";
    [SerializeField]
    public string pullUrl = "http://slothsoft.net/getData.php/sse/server.vct?mode=pull";
    [SerializeField]
    public string roomName = "YourNutz";
    [SerializeField]
    public string playerName = "Ahörnchen";

    void OnValidate() {
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
}
