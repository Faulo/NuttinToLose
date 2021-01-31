using Slothsoft.UnityExtensions;
using UnityEngine;

public class Environment : MonoBehaviour {
    [SerializeField]
    GameObject fallInstance = default;
    [SerializeField]
    GameObject winterInstance = default;
    [SerializeField]
    ServerConnection server = default;

    void Awake() {
        OnValidate();
    }
    void OnEnable() {
        server.onStateEnter += HandleState;
    }
    void OnDisable() {
        server.onStateEnter -= HandleState;
    }
    void HandleState(WorldState state) {
        switch (state) {
            case WorldState.Fall:
            case WorldState.HighScore:
                fallInstance.SetActive(true);
                winterInstance.SetActive(false);
                break;
            case WorldState.Lobby:
            case WorldState.Winter:
                fallInstance.SetActive(false);
                winterInstance.SetActive(true);
                break;
        }
    }
    void Start() {
        fallInstance.SetActive(false);
        winterInstance.SetActive(true);
    }
    void OnValidate() {
        if (!server) {
            server = FindObjectOfType<ServerConnection>();
        }
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
        obj.transform.localPosition = Vector3.zero;
        obj.isStatic = true;
    }
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(Environment))]
    class EnvironmentEditor : RuntimeEditorTools<Environment> {
        protected override void DrawEditorTools() {
            DrawButton("Remove MeshCollider", () => {
                component.SetupCollider(component.fallInstance);
                component.SetupCollider(component.winterInstance);
            });
        }
    }
    void SetupCollider(GameObject obj) {
        if (!obj) {
            return;
        }
        if (obj.TryGetComponent<MeshCollider>(out var collider)) {
            DestroyImmediate(collider);
        }
    }
#endif
}
