using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuitGame", menuName = "ScriptableObjects/QuitGame", order = 1)]
public class QuitGame : ScriptableObject
{
    public void Quit() {
        UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }
}
