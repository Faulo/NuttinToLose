using System;

namespace NuttinToLose {
    [Serializable]
    public class ServerSentEvent {
        public int id;
        public string type;
        public string data;

        public override string ToString() {
            return $"SSE #{id} '{type}':\n{data}";
        }
    }
}