#if !PLATFORM_WEBGL
using System;

namespace NuttinToLose.Networking {
    [Serializable]
    public class ServerICEMessage : ServerMessage {
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
    }
}
#endif