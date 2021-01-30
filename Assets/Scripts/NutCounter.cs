using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class NutCounter : MonoBehaviour {
    [SerializeField]
    GameObject prefabToInstantiate = default;
    [SerializeField]
    List<GameObject> nuts = default;

    void Start() {
        if (nuts == null) {
            nuts = new List<GameObject>();
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
