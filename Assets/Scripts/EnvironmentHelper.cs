using Slothsoft.UnityExtensions;
using UnityEngine;

namespace NuttinToLose {
    public class EnvironmentHelper : MonoBehaviour {
#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(EnvironmentHelper))]
        class EnvironmentEditor : RuntimeEditorTools<EnvironmentHelper> {
            protected override void DrawEditorTools() {
                DrawButton("Update Transforms", () => {
                    UnityEditor.Undo.RegisterFullObjectHierarchyUndo(target.gameObject, "Update Transforms");
                    var envs = target.GetComponentsInChildren<Environment>();
                    foreach (var env in envs) {
                        env.SetupTransform();
                    }
                });
            }
        }
#endif
    }
}