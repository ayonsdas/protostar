using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class Gramophone : MonoBehaviour, IAudioSurface
{
    [SerializeField] private EventReference gramophoneEvent;
    [SerializeField] private Transform emitterTransform;
    private EventInstance eventInstance;

    public void Play(Vector3 playerPosition)
    {
        if (AudioManager.Instance == null || (gramophoneEvent.IsNull && !eventInstance.isValid())) return;

        if (!eventInstance.isValid())
        {
            eventInstance = AudioManager.Instance.CreateEventInstance(gramophoneEvent, emitterTransform.position);
        }

        eventInstance.getPlaybackState(out PLAYBACK_STATE state);
        if (state == PLAYBACK_STATE.STOPPED)
        {
            eventInstance.start();
        }
    }
}