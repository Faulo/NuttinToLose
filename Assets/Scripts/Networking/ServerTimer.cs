using System;
using TMPro;
using UnityEngine;

namespace NuttinToLose.Networking {
    public class ServerTimer : MonoBehaviour {
        [SerializeField]
        TextMeshProUGUI text = default;
        [SerializeField]
        public float time = 0;
        protected void Update() {
            var span = TimeSpan.FromSeconds(time);
            text.text = time >= 0
                ? span.ToString(@"mm\:ss")
                : "";
        }
    }
}