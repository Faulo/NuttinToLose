using UnityEngine;

namespace NuttinToLose {
    public class HighscoreView : MonoBehaviour {
        [SerializeField]
        ServerConnection server = default;
        [SerializeField]
        Transform rowContainer = default;
        [SerializeField]
        HighscoreRow rowPrefab = default;
        void Awake() {
            OnValidate();
        }
        void OnValidate() {
            if (!server) {
                server = GetComponentInParent<ServerConnection>();
            }
        }
        void Start() {
            int i = 1;
            foreach (var data in server.highscores) {
                var row = Instantiate(rowPrefab, rowContainer.transform);
                row.SetValues(i++, data.name, data.nuts);
            }
        }
    }
}