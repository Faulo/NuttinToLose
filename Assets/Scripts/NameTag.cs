using TMPro;
using UnityEngine;

namespace NuttinToLose {
    public class NameTag : MonoBehaviour {
        [SerializeField]
        PlayerController player = default;
        [SerializeField]
        Canvas canvas = default;
        [SerializeField]
        TextMeshProUGUI label = default;
        [SerializeField]
        Camera referenceCamera = default;
        [SerializeField]
        Vector3 offset = Vector3.up;
        void Awake() {
            OnValidate();
        }
        void OnValidate() {
            if (!player) {
                player = GetComponentInParent<PlayerController>();
            }
            if (!canvas) {
                canvas = GetComponentInChildren<Canvas>();
            }
            if (!label) {
                label = GetComponentInChildren<TextMeshProUGUI>();
            }
            if (!referenceCamera) {
                referenceCamera = Camera.main;
            }
        }
        void Start() {
            canvas.worldCamera = referenceCamera;
        }
        void LateUpdate() {
            transform.position = player.transform.position + offset;
            transform.rotation = referenceCamera.transform.rotation;
            label.text = player.data.name;
        }
    }
}