using UnityEngine;

[CreateAssetMenu]
public class PlayerSettings : ScriptableObject {
    [SerializeField, Range(0, 10)]
    public float accelerationDuration = 0;
    [SerializeField, Range(0, 10)]
    public float rotationDuration = 0;
    [SerializeField, Range(0, 100)]
    public float maximumSpeed = 10;
    [SerializeField, Range(0, 10)]
    public float forwardBoost = 0;
    [SerializeField, Range(0, 100)]
    public float upwardsBoost = 0;
    [Header("Physics")]
    [SerializeField, Range(0, 10)]
    public float drag = 0;
    [SerializeField, Range(0, 10)]
    public float gravity = 1;
}
