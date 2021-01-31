using Slothsoft.UnityExtensions;
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
