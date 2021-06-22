#if !PLATFORM_WEBGL
using System;

namespace NuttinToLose {
    [Serializable]
    public class ServerMessage {
        public string from;
        public string to;
    }
}
#endif