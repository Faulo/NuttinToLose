using System.Collections;
using System.Collections.Generic;
using Slothsoft.UnityExtensions;
using UnityEngine;

public class ServerConnection : MonoBehaviour {
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

    [SerializeField, Range(0, 10)]
    float spawnRadius = 1;

    Vector3 spawnPosition;

    void Start() {
        spawnPosition = localPlayer.position;

        localId = System.Guid.NewGuid().ToString();
        localPlayer.data.id = localId;
        localPlayer.data.name = client.playerName;
        spawnedPlayers[localId] = localPlayer;
        client.onMessage += HandleMessage;

        localPlayer.onRealDig += HandleRealDig;
        localPlayer.onFakeDig += HandleFakeDig;
        localPlayer.onDigUp += HandleDigUp;

        StartCoroutine(UpdatePlayerRoutine());

        Invoke(nameof(StartRound), 1);
    }

    public void StartRound() {
        localPlayer.position = spawnPosition;
        localPlayer.nutCount = 10;
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
            id = System.Guid.NewGuid().ToString(),
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
            yield return client.PushRoutine("update", StringifyPlayer(localPlayer));
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
        }
        player.nutCount = data.nuts;
        player.data = data;
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
}
