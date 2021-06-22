using Slothsoft.UnityExtensions;
using UnityEngine;

namespace NuttinToLose {
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
                    target.RemoveCollider(target.fallInstance);
                    target.RemoveCollider(target.winterInstance);
                });
                DrawButton("Update static flags", () => {
                    target.UpdateStaticFlags(target.fallInstance);
                    target.UpdateStaticFlags(target.winterInstance);
                });
            }
        }
        void RemoveCollider(GameObject obj) {
            if (!obj) {
                return;
            }
            if (obj.TryGetComponent<MeshCollider>(out var collider)) {
                DestroyImmediate(collider);
            }
        }
        void UpdateStaticFlags(GameObject obj) {
            if (!obj) {
                return;
            }
            if (!obj.isStatic) {
                obj.isStatic = true;
                UnityEditor.EditorUtility.SetDirty(obj);
            }
#if UNITY_2021
        if (obj.TryGetComponent<Renderer>(out var renderer)) {
            if (!renderer.staticShadowCaster) {
                renderer.staticShadowCaster = true;
                UnityEditor.EditorUtility.SetDirty(renderer);
            }
        }
#endif
        }
#endif
    }
}