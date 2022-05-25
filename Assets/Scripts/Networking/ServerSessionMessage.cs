using System;
using Unity.WebRTC;

namespace NuttinToLose.Networking {
    [Serializable]
    public class ServerSessionMessage : ServerMessage {
        public RTCSdpType type;
        public string sdp;
    }
}