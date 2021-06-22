#if !PLATFORM_WEBGL
using System;
using Unity.WebRTC;

namespace NuttinToLose {
    [Serializable]
    public class ServerSessionMessage : ServerMessage {
        public RTCSdpType type;
        public string sdp;
    }
}
#endif