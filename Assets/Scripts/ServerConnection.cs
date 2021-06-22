using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Slothsoft.UnityExtensions;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.InputSystem;

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

    Dictionary<string, PlayerController> spawnedPlayers = new Dictionary<string, PlayerController>();

    Dictionary<string, DigSpot> digs = new Dictionary<string, DigSpot>();

    [Header("Player Spawn")]
    [SerializeField]
    Transform spawnSpot = default;
    [SerializeField, Range(0, 10)]
    float spawnRadius = 1;
    [SerializeField, Range(1, 100)]
    int startingNutCount = 10;

    [Header("Input")]
    [SerializeField]
    InputAction startAction = new InputAction();

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
                StartCoroutine(client.PushRoutine("world-state", ((int)stateCache).ToString()));
            }
        }
    }
    WorldState stateCache;

    public IEnumerable<PlayerData> highscores => spawnedPlayers.Values
        .Select(player => player.data)
        .OrderByDescending(data => data.nuts);

    void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnSpot.transform.position, spawnRadius);
        if (localPlayer) {
            localPlayer.transform.position = spawnSpot.transform.position;
        }
    }
    void OnEnable() {
    }
    void OnDisable() {
        foreach (var channel in remoteDataChannels.Values) {
            channel.Close();
        }
        foreach (var channel in localDataChannels.Values) {
            channel.Close();
        }
        foreach (var connection in remoteConnections.Values) {
            connection.Close();
        }
        foreach (var connection in localConnections.Values) {
            connection.Close();
        }
        remoteDataChannels.Clear();
        localDataChannels.Clear();
        remoteConnections.Clear();
        localConnections.Clear();
    }

    void Start() {
        StartRTC();

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
            StartCoroutine(client.PushRoutine("remove-dig", StringifyDig(spot)));
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
        StartCoroutine(client.PushRoutine("create-dig", StringifyDig(spot)));
    }

    void HandleRealDig() {
        var data = new DigData() {
            id = System.Guid.NewGuid().ToString(),
            position = localPlayer.position,
            isReal = true
        };
        var spot = CreateDig(data);
        digs[data.id] = spot;
        StartCoroutine(client.PushRoutine("create-dig", StringifyDig(spot)));
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
            yield return client.PushRoutine("update", json);
            foreach (var channel in remoteDataChannels.Values) {
                if (channel.ReadyState == RTCDataChannelState.Open) {
                    channel.Send(json);
                }
            }
            yield return wait;
        }
    }

    void HandleMessage(ServerSentEvent eve) {
        //Debug.Log(eve.type);
        switch (eve.type) {
            case "start":
                StartCoroutine(client.PushRoutine("spawn", StringifyPlayer(localPlayer)));
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
        }
    }

    string StringifyPlayer(PlayerController player) {
        return JsonUtility.ToJson(player.data);
    }
    void ParsePlayer(string text) {
        var data = JsonUtility.FromJson<PlayerData>(text);
        if (IsLocalPlayer(data.id)) {
        } else {
            UpdatePlayer(data);
        }
    }
    void UpdatePlayer(PlayerData data) {
        if (spawnedPlayers.TryGetValue(data.id, out var player)) {
        } else {
            player = Instantiate(playerPrefab, data.position, data.rotation);
            spawnedPlayers[data.id] = player;
            StartCoroutine(CreateRemotePlayerConnectionRoutine(data.id));
        }
        //player.nutCount = data.nuts;
        //player.data = data;
    }
    void UpdatePlayerRTC(byte[] bytes) {
        string json = Encoding.UTF8.GetString(bytes);
        var data = JsonUtility.FromJson<PlayerData>(json);
        if (spawnedPlayers.TryGetValue(data.id, out var player)) {
            player.nutCount = data.nuts;
            player.data = data;
        }
    }
    public bool IsLocalPlayer(string id) => localId == id;

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
    Dictionary<string, RTCPeerConnection> localConnections = new Dictionary<string, RTCPeerConnection>();
    Dictionary<string, RTCDataChannel> localDataChannels = new Dictionary<string, RTCDataChannel>();
    Dictionary<string, RTCPeerConnection> remoteConnections = new Dictionary<string, RTCPeerConnection>();
    Dictionary<string, RTCDataChannel> remoteDataChannels = new Dictionary<string, RTCDataChannel>();
    void StartRTC() {
        WebRTC.Initialize();
    }
    IEnumerator CreateRemotePlayerConnectionRoutine(string id) {
        if (remoteConnections.ContainsKey(id)) {
            yield break;
        }
        remoteConnections[id] = CreateConnection();
        remoteConnections[id].OnIceCandidate += candidate => AddRemoteCandidate(id, candidate);
        remoteDataChannels[id] = remoteConnections[id].CreateDataChannel("data", new RTCDataChannelInit());
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
        yield return client.PushRoutine("rtc-offer", message);
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
        yield return client.PushRoutine("rtc-answer", message);
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
        // TODO: send this to whom it may concern
        var message = new ServerICEMessage {
            from = localId,
            to = remoteId,
            candidate = candidate.Candidate,
            sdpMid = candidate.SdpMid,
            sdpMLineIndex = candidate.SdpMLineIndex ?? 0,
        };
        //Debug.Log($"Sending in the name of {remoteId}:\n{JsonUtility.ToJson(message)}");
        StartCoroutine(client.PushRoutine("rtc-ice", message));
    }
    void ReceiveICEMessage(ServerICEMessage message) {
        string id = message.from;
        CreateLocalPlayerConnection(id);
        var ice = new RTCIceCandidateInit {
            candidate = message.candidate,
            sdpMid = message.sdpMid,
            sdpMLineIndex = message.sdpMLineIndex,
        };
        localConnections[id].AddIceCandidate(new RTCIceCandidate(ice));
    }
    void CreateLocalPlayerConnection(string id) {
        if (localConnections.ContainsKey(id)) {
            return;
        }
        localConnections[id] = CreateConnection();
        localConnections[id].OnDataChannel += channel => {
            localDataChannels[id] = channel;
            channel.OnMessage += UpdatePlayerRTC;
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
    #endregion
}
