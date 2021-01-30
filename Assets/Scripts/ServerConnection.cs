using System.Collections;
using System.Collections.Generic;
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

    Dictionary<string, PlayerController> spawnedPlayers = new Dictionary<string, PlayerController>();


    void Start() {
        localId = System.Guid.NewGuid().ToString();
        localPlayer.data.id = localId;
        spawnedPlayers[localId] = localPlayer;
        client.onMessage += HandleMessage;

        StartCoroutine(UpdatePlayerRoutine());
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
        }
    }

    string StringifyPlayer(PlayerController player) {
        return JsonUtility.ToJson(player.data);
    }
    void ParsePlayer(string text) {
        var data = JsonUtility.FromJson<PlayerData>(text);
        if (IsLocal(data.id)) {

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
        player.data = data;
    }
    public bool IsLocal(string id) => localId == id;
}
