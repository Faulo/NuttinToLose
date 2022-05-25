using System.Collections;
using System.Collections.Generic;

namespace NuttinToLose.Networking {
    public interface IServerSentEventConnection {
        IEnumerator CreatePushRoutine(string uri, string data);
        IEnumerator CreatePollRoutine(string uri, Queue<ServerSentEvent> queue);
    }
}