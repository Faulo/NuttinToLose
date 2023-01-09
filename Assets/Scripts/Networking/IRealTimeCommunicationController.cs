using System;

namespace NuttinToLose.Networking {
    public interface IRealTimeCommunicationController : IDisposable {
        void SetUp(ServerConnection server);

        void ReceiveICEMessage(ServerICEMessage message);
        void ReceiveOfferMessage(ServerSessionMessage message);
        void ReceiveAnswerMessage(ServerSessionMessage message);

        void SpawnPlayer(string id);
        void SendToRemoteChannels(string data);
    }
}