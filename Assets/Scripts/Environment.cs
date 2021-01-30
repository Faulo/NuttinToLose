using UnityEngine;

public class Environment : MonoBehaviour {
    [SerializeField]
    GameObject fallInstance = default;
    [SerializeField]
    GameObject winterInstance = default;
    [SerializeField]
    SeasonManager manager = default;

    void Awake() {
        OnValidate();
    }
    void Start() {
        manager = FindObjectOfType<SeasonManager>();
        if (manager) {
            switch (manager.currentSeason) {
                case Season.Fall:
                    fallInstance.SetActive(true);
                    winterInstance.SetActive(false);
                    break;
                case Season.Winter:
                    fallInstance.SetActive(false);
                    winterInstance.SetActive(true);
                    break;
            }
        }
    }
    void OnValidate() {
        fallInstance = transform.childCount > 0
            ? transform.GetChild(0).gameObject
            : null;
        winterInstance = transform.childCount > 1
            ? transform.GetChild(1).gameObject
            : null;
        SetupEnvironment(fallInstance);
        SetupEnvironment(winterInstance);
    }
    void SetupEnvironment(GameObject obj) {
        if (!obj) {
            return;
        }
        obj.SetActive(true);
        obj.transform.position = Vector3.zero;
        obj.isStatic = true;
        if (obj.TryGetComponent<MeshFilter>(out var filter)) {
            if (!obj.TryGetComponent<MeshCollider>(out var collider)) {
                collider = obj.AddComponent<MeshCollider>();
            }
            collider.sharedMesh = filter.sharedMesh;
        }
    }
}
