using System;

[Serializable]
public class ServerICEMessage : ServerMessage {
    public string candidate;
    public string sdpMid;
    public int sdpMLineIndex;
}