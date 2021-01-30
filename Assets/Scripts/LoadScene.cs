using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "LoadScene", menuName = "ScriptableObjects/LoadScene", order = 1)]
public class LoadScene : ScriptableObject {
    public string sceneToLoad;
    
    public void Load() {
        SceneManager.LoadScene(sceneToLoad);
    }
}
