using UnityEngine;

namespace NuttinToLose {
    [CreateAssetMenu(fileName = "QuitGame", menuName = "ScriptableObjects/QuitGame", order = 1)]
    public class QuitGame : ScriptableObject {
        public void Quit() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }
}