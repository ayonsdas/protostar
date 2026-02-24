using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public enum AudioBus
{
    Master,
    Music,
    SFX
}

public class AudioManager : MonoBehaviour
{
    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;

    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;
    private EventInstance ambienceEventInstance;
    private EventInstance musicEventInstance;

    public static AudioManager Instance { get; private set; }
    public void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }

        Instance = this;

        eventInstances = new List<EventInstance>();
        eventEmitters = new List<StudioEventEmitter>();

        masterBus = RuntimeManager.GetBus("bus:/");
        musicBus = RuntimeManager.GetBus("bus:/Music Bus");
        sfxBus = RuntimeManager.GetBus("bus:/SFX Bus");

        InitializeBusVolumes();
    }

    private void InitializeBusVolumes()
    {
        float masterVolume = PlayerPrefs.GetFloat(PlayerPrefKeys.MASTER_VOLUME_KEY, 1f);
        SetBusVolume(AudioBus.Master, masterVolume);

        float musicVolume = PlayerPrefs.GetFloat(PlayerPrefKeys.MUSIC_VOLUME_KEY, 1f);
        SetBusVolume(AudioBus.Music, musicVolume);

        float sfxVolume = PlayerPrefs.GetFloat(PlayerPrefKeys.SFX_VOLUME_KEY, 1f);
        SetBusVolume(AudioBus.SFX, sfxVolume);
    }

    public void SetBusVolume(AudioBus bus, float volume)
    {
        switch (bus)
        {
            case AudioBus.Master:
                masterBus.setVolume(volume);
                break;
            case AudioBus.Music:
                musicBus.setVolume(volume);
                break;
            case AudioBus.SFX:
                sfxBus.setVolume(volume);
                break;
        }
    }

    public void PlayOneShot(EventReference eventReference, Vector3 position)
    {
        RuntimeManager.PlayOneShot(eventReference, position);
    }

    public EventInstance CreateEventInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public StudioEventEmitter InitializeEventEmitter(EventReference eventReference, GameObject emitter)
    {
        StudioEventEmitter eventEmitter = emitter.GetComponent<StudioEventEmitter>();
        eventEmitter.EventReference = eventReference;
        eventEmitters.Add(eventEmitter);
        return eventEmitter;
    }

    public void PlayMusic(EventReference eventReference)
    {
        musicEventInstance = CreateEventInstance(eventReference);
        musicEventInstance.start();
    }

    public void SetMusicActive(bool active, FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.IMMEDIATE)
    {
        if (active)
        {
            musicEventInstance.start();
        }
        else
        {
            musicEventInstance.stop(stopMode);
        }
    }

    public void SetMusicParameter(string parameterName, float value)
    {
        musicEventInstance.setParameterByName(parameterName, value);
        Debug.Log($"[AudioManager] Set music parameter {parameterName} to {value}");
    }

    public void PlayAmbience(EventReference eventReference)
    {
        ambienceEventInstance = CreateEventInstance(eventReference);
        ambienceEventInstance.start();
    }

    public void SetAmbienceActive(bool active, FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.IMMEDIATE)
    {
        if (active)
        {
            ambienceEventInstance.start();
        }
        else
        {
            ambienceEventInstance.stop(stopMode);
        }
    }

    private void Cleanup()
    {
        if (eventInstances != null)
        {
            foreach (EventInstance eventInstance in eventInstances)
            {
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
        }
        if (eventEmitters != null)
        {
            foreach (StudioEventEmitter emitter in eventEmitters)
            {
                emitter.Stop();
            }
        }
    }


    private void OnDestroy()
    {
        Cleanup();
    }
}