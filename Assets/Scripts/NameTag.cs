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
        protected void Awake() {
            OnValidate();
        }
        protected void OnValidate() {
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
        protected void Start() {
            canvas.worldCamera = referenceCamera;
        }
        protected void LateUpdate() {
            transform.SetPositionAndRotation(player.transform.position + offset, referenceCamera.transform.rotation);
            label.text = player.data.name;
        }
    }
}