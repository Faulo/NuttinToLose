using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class NutCounter : MonoBehaviour {
    [SerializeField]
    PlayerController player = default;
    [SerializeField]
    GameObject prefabToInstantiate = default;
    [SerializeField]
    List<GameObject> nuts = new List<GameObject>();

    void Start() {
        player.onNutChange += HandleNutChange;
    }

    public void HandleNutChange(int count) {
        if (count > nuts.Count) {
            AddNuts(count - nuts.Count);
        }
        if (nuts.Count > count) {
            ReduceNuts(nuts.Count - count);
        }
    }

    public void AddNuts(int count) {
        Assert.IsTrue(count > 0);
        for (int i = 0; i < count; i++) {
            nuts.Add(Instantiate(prefabToInstantiate, transform));
        }
    }

    public void ReduceNuts(int count) {
        Assert.IsTrue(count > 0);
        Assert.IsTrue(nuts.Count > 0);
        for (int i = 1; i < count + 1; i++) {
            Destroy(nuts[nuts.Count - i]);
        }
        nuts.RemoveRange(nuts.Count - count, count);
    }
}
