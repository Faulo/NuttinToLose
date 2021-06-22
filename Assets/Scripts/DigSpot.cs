using UnityEngine;

namespace NuttinToLose {
    public class DigSpot : MonoBehaviour {
        public DigData data;

        public void GetDugUp() {
            Destroy(gameObject);
        }
    }
}