#if !PLATFORM_WEBGL
using System;

namespace NuttinToLose.Networking {
    [Serializable]
    public class ServerMessage {
        public string from;
        public string to;
    }
}
#endif