using UnityEngine;

public class PlayerEvents : MonoBehaviour {
    [SerializeField]
    PlayerController player = default;
    [SerializeField]
    GameObjectEvent onRealDig = new GameObjectEvent();
    [SerializeField]
    GameObjectEvent onFakeDig = new GameObjectEvent();
    [SerializeField]
    GameObjectEvent onDigUpReal = new GameObjectEvent();
    [SerializeField]
    GameObjectEvent onDigUpFake = new GameObjectEvent();
    [SerializeField]
    GameObjectEvent onNutCountUp = new GameObjectEvent();
    [SerializeField]
    GameObjectEvent onNutCountDown = new GameObjectEvent();

    void Awake() {
        OnValidate();
    }
    void OnValidate() {
        if (!player) {
            player = GetComponentInParent<PlayerController>();
        }
    }
    void Start() {
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
