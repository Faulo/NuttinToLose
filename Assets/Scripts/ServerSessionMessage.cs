using System;
using Unity.WebRTC;

[Serializable]
public class ServerSessionMessage : ServerMessage {
    public RTCSdpType type;
    public string sdp;
}