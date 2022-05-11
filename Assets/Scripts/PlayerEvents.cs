using UnityEngine;

namespace NuttinToLose {
    public class PlayerEvents : MonoBehaviour {
        [SerializeField]
        PlayerController player = default;
        [SerializeField]
        GameObjectEvent onRealDig = new();
        [SerializeField]
        GameObjectEvent onFakeDig = new();
        [SerializeField]
        GameObjectEvent onDigUpReal = new();
        [SerializeField]
        GameObjectEvent onDigUpFake = new();
        [SerializeField]
        GameObjectEvent onNutCountUp = new();
        [SerializeField]
        GameObjectEvent onNutCountDown = new();

        protected void Awake() {
            OnValidate();
        }
        protected void OnValidate() {
            if (!player) {
                player = GetComponentInParent<PlayerController>();
            }
        }
        protected void Start() {
            player.onRealDig += () => onRealDig.Invoke(player.gameObject);
            player.onFakeDig += () => onFakeDig.Invoke(player.gameObject);
            player.onDigUp += spot => {
                if (spot.data.isReal) {
                    onDigUpReal.Invoke(player.gameObject);
                } else {
                    onDigUpFake.Invoke(player.gameObject);
                }
            };
            player.onNutChange += count => {
                if (count > player.nutCount) {
                    onNutCountUp.Invoke(player.gameObject);
                } else {
                    onNutCountDown.Invoke(player.gameObject);
                }
            };
        }
    }
}