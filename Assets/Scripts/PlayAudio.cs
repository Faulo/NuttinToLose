using Slothsoft.UnityExtensions;
using UnityEngine;

[CreateAssetMenu]
public class PlayAudio : ScriptableObject {
    [SerializeField, Expandable]
    AudioSource audioPrefab = default;
    [SerializeField, Range(0, 10)]
    float offset = 0;
    [SerializeField, Range(0, 3)]
    float minimumPitch = 1;
    [SerializeField, Range(0, 3)]
    float maximumPitch = 1;
    [SerializeField, Range(0, 3)]
    float minimumVolume = 1;
    [SerializeField, Range(0, 3)]
    float maximumVolume = 1;

    public void Dispatch(GameObject context) {
        var audioInstance = Instantiate(audioPrefab, context.transform);
        audioInstance.time = offset;
        audioInstance.pitch = Random.Range(minimumPitch, maximumPitch);
        audioInstance.volume = Random.Range(minimumVolume, maximumVolume);
        audioInstance.Play();
        Destroy(audioInstance.gameObject, audioInstance.clip.length);
    }
}
