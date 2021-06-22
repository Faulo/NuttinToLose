using Cinemachine;
using UnityEngine;

namespace NuttinToLose {
    public class CinemachineAxisInput : MonoBehaviour, AxisState.IInputAxisProvider {
        public Vector2 input = Vector2.zero;
        public float GetAxisValue(int axis) {
            return input[axis];
        }
    }
}