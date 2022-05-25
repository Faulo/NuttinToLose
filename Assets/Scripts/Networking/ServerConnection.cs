using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Slothsoft.UnityExtensions;
#if !PLATFORM_WEBGL
using Unity.WebRTC;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

namespace NuttinToLose.Networking {
    public class ServerConnection : MonoBehaviour {
        public event Action<WorldState> onStateEnter;
        public event Action<WorldState> onStateExit;

        [SerializeField]
        ServerSentEventClient client = default;
        [SerializeField]
        PlayerController localPlayer = default;
        [SerializeField]
        string localId = default;
        [SerializeField]
        PlayerController playerPrefab = default;
        [SerializeField, Range(0, 5)]
        float updateInterval = 1;
        [SerializeField, Expandable]
        DigSpot spotPrefab = default;

        Dictionary<string, PlayerController> spawnedPlayers = new();

        Dictionary<string, DigSpot> digs = new();

        [Header("Player Spawn")]
        [SerializeField]
        Transform spawnSpot = default;
        [SerializeField, Range(0, 10)]
        float spawnRadius = 1;
        [SerializeField, Range(1, 100)]
        int startingNutCount = 10;

        [Header("Input")]
        [SerializeField]
        InputAction startAction = new();

        [Header("Time")]
        [SerializeField, Range(0, 1000)]
        float fallDuration = 120;
        [SerializeField, Range(0, 1000)]
        float winterDuration = 120;
        [SerializeField]
        ServerTimer timerInstance = default;

        [Header("Highscore")]
        [SerializeField]
        GameObject highscoreInstance = default;

        public WorldState state {
            get => stateCache;
            private set {
                if (stateCache < value) {
                    onStateExit?.Invoke(stateCache);
                    stateCache = value;
                    onStateEnter?.Invoke(stateCache);
                    client.PushMessage("world-state", ((int)stateCache).ToString());
                }
            }
        }
        WorldState stateCache;

        public IEnumerable<PlayerData> highscores => spawnedPlayers.Values
            .Select(player => player.data)
            .OrderByDescending(data => data.nuts);

        protected void OnDrawGizmos() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnSpot.transform.position, spawnRadius);
            if (localPlayer) {
                localPlayer.transform.position = spawnSpot.transform.position;
            }
        }
        protected void OnEnable() {
#if !PLATFORM_WEBGL
            StartRTC();
#endif
        }
        protected void OnDisable() {
#if !PLATFORM_WEBGL
            StopRTC();
#endif
        }

        protected void Start() {
            localId = Guid.NewGuid().ToString();
            localPlayer.data.id = localId;
            localPlayer.data.name = client.playerName;
            spawnedPlayers[localId] = localPlayer;
            client.onMessage += HandleMessage;

            localPlayer.onRealDig += HandleRealDig;
            localPlayer.onFakeDig += HandleFakeDig;
            localPlayer.onDigUp += HandleDigUp;

            onStateEnter += HandleEnterState;
            onStateExit += HandleExitState;

            startAction.performed += context => {
                state = WorldState.Fall;
            };

            StartCoroutine(UpdatePlayerRoutine());

            state = WorldState.Lobby;
        }

        void HandleEnterState(WorldState state) {
            switch (state) {
                case WorldState.Inactive:
                    break;
                case WorldState.Lobby:
                    startAction.Enable();
                    ResetPlayer();
                    break;
                case WorldState.Fall:
                    localPlayer.nutCount = startingNutCount;
                    ResetPlayer();
                    StartCoroutine(StartTimer(fallDuration, WorldState.Winter));
                    break;
                case WorldState.Winter:
                    localPlayer.nutCount = 0;
                    ResetPlayer();
                    StartCoroutine(StartTimer(winterDuration, WorldState.HighScore));
                    break;
                case WorldState.HighScore:
                    ResetPlayer();
                    highscoreInstance.SetActive(true);
                    break;
                default:
                    throw new NotImplementedException(state.ToString());
            }
        }
        IEnumerator StartTimer(float duration, WorldState state) {
            timerInstance.gameObject.SetActive(true);
            timerInstance.time = duration;
            while (timerInstance.time > 0) {
                yield return null;
                timerInstance.time -= Time.deltaTime;
            }
            timerInstance.gameObject.SetActive(false);
            yield return new WaitForFixedUpdate();
            this.state = state;
        }
        void HandleExitState(WorldState state) {
            switch (state) {
                case WorldState.Inactive:
                    break;
                case WorldState.Lobby:
                    startAction.Disable();
                    break;
                case WorldState.Fall:
                    break;
                case WorldState.Winter:
                    break;
                case WorldState.HighScore:
                    break;
                default:
                    throw new NotImplementedException(state.ToString());
            }
        }

        void ResetPlayer() {
            var position = spawnSpot.position + (UnityEngine.Random.insideUnitSphere * spawnRadius);
            localPlayer.TeleportTo(position, Quaternion.Euler(0, UnityEngine.Random.Range(-180, 180), 0));
        }

        void HandleDigUp(DigSpot spot) {
            string id = spot.data.id;
            if (digs.Remove(id)) {
                client.PushMessage("remove-dig", StringifyDig(spot));
            }
            spot.GetDugUp();
        }

        void HandleFakeDig() {
            var data = new DigData() {
                id = Guid.NewGuid().ToString(),
                position = localPlayer.position,
                isReal = false
            };
            var spot = CreateDig(data);
            digs[data.id] = spot;
            client.PushMessage("create-dig", StringifyDig(spot));
        }

        void HandleRealDig() {
            var data = new DigData() {
                id = System.Guid.NewGuid().ToString(),
                position = localPlayer.position,
                isReal = true
            };
            var spot = CreateDig(data);
            digs[data.id] = spot;
            client.PushMessage("create-dig", StringifyDig(spot));
        }

        DigSpot CreateDig(DigData data) {
            var spot = Instantiate(spotPrefab, data.position, Quaternion.identity);
            spot.data = data;
            return spot;
        }

        IEnumerator UpdatePlayerRoutine() {
            var wait = new WaitForSeconds(updateInterval);
            while (true) {
                string json = StringifyPlayer(localPlayer);
                yield return client.PushMessage("update", json);
                yield return wait;
            }
        }

        void HandleMessage(ServerSentEvent eve) {
            //Debug.Log(eve.type);
            switch (eve.type) {
                case "start":
                    client.PushMessage("spawn", StringifyPlayer(localPlayer));
                    break;
                case "spawn":
                case "update":
                    ParsePlayer(eve.data);
                    break;
                case "create-dig":
                    ParseCreateDig(eve.data);
                    break;
                case "remove-dig":
                    ParseRemoveDig(eve.data);
                    break;
                case "world-state":
                    state = (WorldState)int.Parse(eve.data);
                    break;
#if !PLATFORM_WEBGL
                case "rtc-ice": {
                    var message = JsonUtility.FromJson<ServerICEMessage>(eve.data);
                    if (message.to == localId) {
                        ReceiveICEMessage(message);
                    }
                    break;
                }
                case "rtc-offer": {
                    var message = JsonUtility.FromJson<ServerSessionMessage>(eve.data);
                    if (message.to == localId) {
                        ReceiveOfferMessage(message);
                    }
                    break;
                }
                case "rtc-answer": {
                    var message = JsonUtility.FromJson<ServerSessionMessage>(eve.data);
                    if (message.to == localId) {
                        ReceiveAnswerMessage(message);
                    }
                    break;
                }
#endif
            }
        }

        string StringifyPlayer(PlayerController player) {
            return JsonUtility.ToJson(player.data);
        }
        void ParsePlayer(string text) {
            var data = JsonUtility.FromJson<PlayerData>(text);
            if (!spawnedPlayers.ContainsKey(data.id)) {
                SpawnPlayer(data);
            }
        }
        void SpawnPlayer(PlayerData data) {
            var player = Instantiate(playerPrefab, data.position, data.rotation);
            spawnedPlayers[data.id] = player;
#if !PLATFORM_WEBGL
            StartCoroutine(CreateRemotePlayerConnectionRoutine(data.id));
#endif
        }
#if !PLATFORM_WEBGL
        void UpdatePlayer(byte[] bytes) {
            string json = Encoding.UTF8.GetString(bytes);
            var data = JsonUtility.FromJson<PlayerData>(json);
            if (spawnedPlayers.TryGetValue(data.id, out var player)) {
                player.nutCount = data.nuts;
                player.data = data;
            }
        }
#endif
        string StringifyDig(DigSpot spot) {
            return JsonUtility.ToJson(spot.data);
        }
        void ParseCreateDig(string text) {
            var data = JsonUtility.FromJson<DigData>(text);
            if (!digs.ContainsKey(data.id)) {
                digs[data.id] = CreateDig(data);
            }
        }
        void ParseRemoveDig(string text) {
            var data = JsonUtility.FromJson<DigData>(text);
            if (digs.TryGetValue(data.id, out var spot)) {
                digs.Remove(data.id);
                spot.GetDugUp();
            }
        }




        #region WebRTC
#if !PLATFORM_WEBGL
        [Header("WebRTC")]
        [SerializeField]
        bool captureAudio = false;
        [SerializeField, Range(0, 1)]
        float updateRTCInterval = 0;
        Dictionary<string, RTCPeerConnection> localConnections = new Dictionary<string, RTCPeerConnection>();
        Dictionary<string, RTCDataChannel> localDataChannels = new Dictionary<string, RTCDataChannel>();
        Dictionary<string, MediaStreamTrack> localTracks = new Dictionary<string, MediaStreamTrack>();
        Dictionary<string, RTCPeerConnection> remoteConnections = new Dictionary<string, RTCPeerConnection>();
        Dictionary<string, RTCDataChannel> remoteDataChannels = new Dictionary<string, RTCDataChannel>();
        MediaStream audioStream;
        void StartRTC() {
            WebRTC.Initialize();
            if (captureAudio) {
                audioStream = Audio.CaptureStream();
            }
            StartCoroutine(UpdateChannelRoutine());
        }
        IEnumerator PushMessageRoutine(string type, ServerMessage data) => client.PushRoutine(type, JsonUtility.ToJson(data));
        IEnumerator UpdateChannelRoutine() {
            var wait = new WaitForSeconds(updateRTCInterval);
            while (true) {
                string json = StringifyPlayer(localPlayer);
                foreach (var channel in remoteDataChannels.Values) {
                    if (channel.ReadyState == RTCDataChannelState.Open) {
                        channel.Send(json);
                    }
                }
                yield return wait;
            }
        }
        void StopRTC() {
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
            WebRTC.Dispose();
        }
        IEnumerator CreateRemotePlayerConnectionRoutine(string id) {
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
            yield return CreateOfferSuccess(id, op.Desc);
        }
        IEnumerator CreateOfferSuccess(string remoteId, RTCSessionDescription desc) {
            var op = remoteConnections[remoteId].SetLocalDescription(ref desc);
            yield return op;
            if (op.IsError) {
                Debug.LogError(op.Error);
                yield break;
            }
            var message = new ServerSessionMessage {
                from = localId,
                to = remoteId,
                type = desc.type,
                sdp = desc.sdp,
            };
            //Debug.Log($"Sending offer:\n{JsonUtility.ToJson(message)}");
            yield return PushMessageRoutine("rtc-offer", message);
        }
        void ReceiveOfferMessage(ServerSessionMessage message) {
            string id = message.from;
            CreateLocalPlayerConnection(id);
            var desc = new RTCSessionDescription {
                type = message.type,
                sdp = message.sdp,
            };
            StartCoroutine(ReceiveOfferMessageRoutine(id, desc));
        }
        IEnumerator ReceiveOfferMessageRoutine(string remoteId, RTCSessionDescription desc) {
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
            var message = new ServerSessionMessage {
                from = localId,
                to = remoteId,
                type = desc.type,
                sdp = desc.sdp,
            };
            //Debug.Log($"Sending answer:\n{JsonUtility.ToJson(message)}");
            yield return PushMessageRoutine("rtc-answer", message);
        }
        void ReceiveAnswerMessage(ServerSessionMessage message) {
            string remoteId = message.from;
            var desc = new RTCSessionDescription {
                type = message.type,
                sdp = message.sdp,
            };
            remoteConnections[remoteId].SetRemoteDescription(ref desc);
            //Debug.Log($"Set remote description {desc} to {remoteId}!");
        }
        void AddRemoteCandidate(string remoteId, RTCIceCandidate candidate) {
            var message = new ServerICEMessage {
                from = localId,
                to = remoteId,
                candidate = candidate.Candidate,
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0,
            };
            //Debug.Log($"Sending in the name of {remoteId}:\n{JsonUtility.ToJson(message)}");
            if (localConnections.TryGetValue(remoteId, out var connection)) {
                connection.AddIceCandidate(candidate);
            }
            if (remoteConnections.TryGetValue(remoteId, out connection)) {
                connection.AddIceCandidate(candidate);
            }
            StartCoroutine(PushMessageRoutine("rtc-ice", message));
        }
        void ReceiveICEMessage(ServerICEMessage message) {
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
                        channel.OnMessage += UpdatePlayer;
                        Debug.Log($"Established data channel with {id}: {channel}");
                        break;
                    default:
                        throw new NotImplementedException($"Dunno what to do with channel '{channel.Label}'");
                }
            };
        }
        IEnumerator CreateLocalPlayerAnswerRoutine(string id) {
            var op = localConnections[id].CreateAnswer();
            yield return op;
            if (op.IsError) {
                Debug.LogError(op.Error);
                yield break;
            }
            yield return CreateOfferSuccess(id, op.Desc);
        }
        public RTCConfiguration CreateConfiguration() {
            var config = new RTCConfiguration {
                iceServers = new[] {
                new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
            }
            };
            return config;
        }
        public RTCPeerConnection CreateConnection() {
            var configuration = CreateConfiguration();
            var connection = new RTCPeerConnection(ref configuration);
            //connection.OnIceConnectionChange += OnIceConnectionChange;
            return connection;
        }
        void OnIceConnectionChange(RTCIceConnectionState state) {
            Debug.Log($"OnIceConnectionChange: {state}");
        }
#endif
        #endregion
    }
}