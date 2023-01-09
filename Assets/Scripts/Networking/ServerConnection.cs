using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Slothsoft.UnityExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NuttinToLose.Networking {
    public class ServerConnection : MonoBehaviour {
        public event Action<WorldState> onStateEnter;
        public event Action<WorldState> onStateExit;

        [SerializeField]
        ServerSentEventClient client = default;
        public Coroutine PushMessage(string type, ServerMessage data) => client.PushMessage(type, JsonUtility.ToJson(data));

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

        IRealTimeCommunicationController rtc;

        protected void OnEnable() {
#if PLATFORM_WEBGL
#else
            rtc = new RTCUnity();
#endif
            rtc?.SetUp(this);
        }

        protected void OnDisable() {
            rtc?.Dispose();
            rtc = null;
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

            StartCoroutine(UpdatePlayer_Co());

            state = WorldState.Lobby;
        }

        IEnumerator UpdatePlayer_Co() {
            var wait = new WaitForSeconds(updateInterval);
            while (true) {
                string json = StringifyPlayer(localPlayer);
                rtc?.SendToRemoteChannels(json);
                yield return client.PushMessage("update", json);
                yield return wait;
            }
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
                    StartCoroutine(StartTimer_Co(fallDuration, WorldState.Winter));
                    break;
                case WorldState.Winter:
                    localPlayer.nutCount = 0;
                    ResetPlayer();
                    StartCoroutine(StartTimer_Co(winterDuration, WorldState.HighScore));
                    break;
                case WorldState.HighScore:
                    ResetPlayer();
                    highscoreInstance.SetActive(true);
                    break;
                default:
                    throw new NotImplementedException(state.ToString());
            }
        }

        IEnumerator StartTimer_Co(float duration, WorldState state) {
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
                case "rtc-ice": {
                    var message = JsonUtility.FromJson<ServerICEMessage>(eve.data);
                    if (message.to == localId) {
                        rtc?.ReceiveICEMessage(message);
                    }
                    break;
                }
                case "rtc-offer": {
                    var message = JsonUtility.FromJson<ServerSessionMessage>(eve.data);
                    if (message.to == localId) {
                        rtc?.ReceiveOfferMessage(message);
                    }
                    break;
                }
                case "rtc-answer": {
                    var message = JsonUtility.FromJson<ServerSessionMessage>(eve.data);
                    if (message.to == localId) {
                        rtc?.ReceiveAnswerMessage(message);
                    }
                    break;
                }
            }
        }

        static string StringifyPlayer(PlayerController player) {
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
            rtc?.SpawnPlayer(data.id);
        }
        public void UpdatePlayer(PlayerData data) {
            if (spawnedPlayers.TryGetValue(data.id, out var player)) {
                player.nutCount = data.nuts;
                player.data = data;
            }
        }
        static string StringifyDig(DigSpot spot) {
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

        public T CreateMessageForPlayer<T>(string remoteId)
            where T : ServerMessage, new() {
            return new T {
                from = localId,
                to = remoteId,
            };
        }
    }
}