using System;
using UnityEngine;
using UnityEngine.Events;

namespace NuttinToLose {
    [Serializable]
    public class GameObjectEvent : UnityEvent<GameObject> {
    }
}