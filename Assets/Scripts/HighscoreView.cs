using NuttinToLose.Networking;
using UnityEngine;

namespace NuttinToLose {
    public class HighscoreView : MonoBehaviour {
        [SerializeField]
        ServerConnection server = default;
        [SerializeField]
        Transform rowContainer = default;
        [SerializeField]
        HighscoreRow rowPrefab = default;
        protected void Awake() {
            OnValidate();
        }
        protected void OnValidate() {
            if (!server) {
                server = GetComponentInParent<ServerConnection>();
            }
        }
        protected void Start() {
            int i = 1;
            foreach (var data in server.highscores) {
                var row = Instantiate(rowPrefab, rowContainer.transform);
                row.SetValues(i++, data.name, data.nuts);
            }
        }
    }
}