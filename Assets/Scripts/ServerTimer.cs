using System;
using TMPro;
using UnityEngine;

namespace NuttinToLose {
    public class ServerTimer : MonoBehaviour {
        [SerializeField]
        TextMeshProUGUI text = default;
        [SerializeField]
        public float time = 0;
        void Update() {
            var span = TimeSpan.FromSeconds(time);
            text.text = time >= 0
                ? span.ToString(@"mm\:ss")
                : "";
        }
    }
}