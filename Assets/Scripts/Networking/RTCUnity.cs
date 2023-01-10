#if !UNITY_WEBGL
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.WebRTC;
using UnityEngine;
#endif

namespace NuttinToLose.Networking {
    public class RTCUnity : IRealTimeCommunicationController {
#if UNITY_WEBGL
        public void Dispose() {
        }
        public void ReceiveAnswerMessage(ServerSessionMessage message) {
        }
        public void ReceiveICEMessage(ServerICEMessage message) {
        }
        public void ReceiveOfferMessage(ServerSessionMessage message) {
        }
        public void SendToRemoteChannels(string data) {
        }
        public void SetUp(ServerConnection server) {
        }
        public void SpawnPlayer(string id) {
        }
#else
        [Header("WebRTC")]
        [SerializeField]
        bool captureAudio = false;

        readonly Dictionary<string, RTCPeerConnection> localConnections = new();
        readonly Dictionary<string, RTCDataChannel> localDataChannels = new();
        readonly Dictionary<string, MediaStreamTrack> localTracks = new();
        readonly Dictionary<string, RTCPeerConnection> remoteConnections = new();
        readonly Dictionary<string, RTCDataChannel> remoteDataChannels = new();

        ServerConnection server;
        MediaStream audioStream;

        public void SetUp(ServerConnection server) {
            this.server = server;

            if (captureAudio) {
                //audioStream = Audio.CaptureStream();
            }
        }

        public void Dispose() {
            foreach (var channel in remoteDataChannels.Values) {
                channel.Close();
                channel.Dispose();
            }
            remoteDataChannels.Clear();
            foreach (var channel in localDataChannels.Values) {
                channel.Close();
                channel.Dispose();
            }
            localDataChannels.Clear();
            foreach (var connection in remoteConnections.Values) {
                connection.Close();
                connection.Dispose();
            }
            remoteConnections.Clear();
            foreach (var connection in localConnections.Values) {
                connection.Close();
                connection.Dispose();
            }
            localConnections.Clear();
            foreach (var track in localTracks.Values) {
                track.Stop();
                track.Dispose();
            }
            localTracks.Clear();
        }

        public void SpawnPlayer(string id) {
            server.StartCoroutine(CreateRemotePlayerConnection_Co(id));
        }

        IEnumerator CreateRemotePlayerConnection_Co(string id) {
            if (remoteConnections.ContainsKey(id)) {
                yield break;
            }

            remoteConnections[id] = CreateConnection();
            remoteConnections[id].OnIceCandidate += candidate => AddRemoteCandidate(id, candidate);
            remoteDataChannels[id] = remoteConnections[id].CreateDataChannel("data", new RTCDataChannelInit());

            if (audioStream != null) {
                foreach (var track in audioStream.GetTracks()) {
                    remoteConnections[id].AddTrack(track, audioStream);
                }
            }

            var op = remoteConnections[id].CreateOffer();
            yield return op;
            if (op.IsError) {
                Debug.LogError(op.Error);
                yield break;
            }

            yield return CreateOfferSuccess_Co(id, op.Desc);
        }

        IEnumerator CreateOfferSuccess_Co(string remoteId, RTCSessionDescription desc) {
            var op = remoteConnections[remoteId].SetLocalDescription(ref desc);

            yield return op;
            if (op.IsError) {
                Debug.LogError(op.Error);
                yield break;
            }

            var message = server.CreateMessageForPlayer<ServerSessionMessage>(remoteId);
            message.type = (int)desc.type;
            message.sdp = desc.sdp;

            //Debug.Log($"Sending offer:\n{JsonUtility.ToJson(message)}");

            yield return server.PushMessage("rtc-offer", message);
        }

        public void ReceiveOfferMessage(ServerSessionMessage message) {
            string id = message.from;
            CreateLocalPlayerConnection(id);
            var desc = new RTCSessionDescription {
                type = (RTCSdpType)message.type,
                sdp = message.sdp,
            };
            server.StartCoroutine(ReceiveOfferMessage_Co(id, desc));
        }

        IEnumerator ReceiveOfferMessage_Co(string remoteId, RTCSessionDescription desc) {
            localConnections[remoteId].SetRemoteDescription(ref desc);
            var op = localConnections[remoteId].CreateAnswer();

            yield return op;
            if (op.IsError) {
                Debug.LogError(op.Error);
                yield break;
            }

            desc = op.Desc;
            var op2 = localConnections[remoteId].SetLocalDescription(ref desc);
            yield return op2;
            if (op2.IsError) {
                Debug.LogError(op2.Error);
                yield break;
            }

            var message = server.CreateMessageForPlayer<ServerSessionMessage>(remoteId);
            message.type = (int)desc.type;
            message.sdp = desc.sdp;

            //Debug.Log($"Sending answer:\n{JsonUtility.ToJson(message)}");

            yield return server.PushMessage("rtc-answer", message);
        }

        public void ReceiveAnswerMessage(ServerSessionMessage message) {
            string remoteId = message.from;
            var desc = new RTCSessionDescription {
                type = (RTCSdpType)message.type,
                sdp = message.sdp,
            };
            remoteConnections[remoteId].SetRemoteDescription(ref desc);
            //Debug.Log($"Set remote description {desc} to {remoteId}!");
        }

        void AddRemoteCandidate(string remoteId, RTCIceCandidate candidate) {
            var message = server.CreateMessageForPlayer<ServerICEMessage>(remoteId);

            message.candidate = candidate.Candidate;
            message.sdpMid = candidate.SdpMid;
            message.sdpMLineIndex = candidate.SdpMLineIndex ?? 0;

            //Debug.Log($"Sending in the name of {remoteId}:\n{JsonUtility.ToJson(message)}");
            if (localConnections.TryGetValue(remoteId, out var connection)) {
                connection.AddIceCandidate(candidate);
            }
            if (remoteConnections.TryGetValue(remoteId, out connection)) {
                connection.AddIceCandidate(candidate);
            }

            server.PushMessage("rtc-ice", message);
        }

        public void ReceiveICEMessage(ServerICEMessage message) {
            string id = message.from;
            CreateLocalPlayerConnection(id);
            var ice = new RTCIceCandidateInit {
                candidate = message.candidate,
                sdpMid = message.sdpMid,
                sdpMLineIndex = message.sdpMLineIndex,
            };
            var candidate = new RTCIceCandidate(ice);
            if (localConnections.TryGetValue(id, out var connection)) {
                connection.AddIceCandidate(candidate);
            }
            if (remoteConnections.TryGetValue(id, out connection)) {
                connection.AddIceCandidate(candidate);
            }
        }

        void CreateLocalPlayerConnection(string id) {
            if (localConnections.ContainsKey(id)) {
                return;
            }
            localConnections[id] = CreateConnection();
            localConnections[id].OnIceCandidate += candidate => AddRemoteCandidate(id, candidate);
            localConnections[id].OnTrack += eve => {
                Debug.Log($"Received track {eve.Track.Id} of kind {eve.Track.Kind}");
                localTracks[id] = eve.Track;
            };
            localConnections[id].OnDataChannel += channel => {
                switch (channel.Label) {
                    case "data":
                        localDataChannels[id] = channel;
                        channel.OnMessage += HandlePlayerUpdate;
                        Debug.Log($"Established data channel with {id}: {channel}");
                        break;
                    default:
                        throw new NotImplementedException($"Dunno what to do with channel '{channel.Label}'");
                }
            };
        }

        void HandlePlayerUpdate(byte[] bytes) {
            string json = Encoding.UTF8.GetString(bytes);
            var data = JsonUtility.FromJson<PlayerData>(json);
            server.UpdatePlayer(data);
        }

        IEnumerator CreateLocalPlayerAnswer_Co(string id) {
            var op = localConnections[id].CreateAnswer();
            yield return op;
            if (op.IsError) {
                Debug.LogError(op.Error);
                yield break;
            }
            yield return CreateOfferSuccess_Co(id, op.Desc);
        }

        RTCConfiguration CreateConfiguration() {
            var config = new RTCConfiguration {
                iceServers = new[] {
                    new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
                }
            };
            return config;
        }

        RTCPeerConnection CreateConnection() {
            var configuration = CreateConfiguration();
            var connection = new RTCPeerConnection(ref configuration);
            connection.OnIceConnectionChange += OnIceConnectionChange;
            return connection;
        }

        void OnIceConnectionChange(RTCIceConnectionState state) {
            Debug.Log($"OnIceConnectionChange: {state}");
        }

        public void SendToRemoteChannels(string data) {
            foreach (var channel in remoteDataChannels.Values) {
                if (channel.ReadyState == RTCDataChannelState.Open) {
                    channel.Send(data);
                }
            }
        }
#endif
    }
}