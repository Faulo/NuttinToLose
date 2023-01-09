using System;

namespace NuttinToLose.Networking {
    [Serializable]
    public class ServerSessionMessage : ServerMessage {
        public int type;
        public string sdp;
    }
}