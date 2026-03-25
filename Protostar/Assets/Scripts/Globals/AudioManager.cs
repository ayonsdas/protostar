using System;
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
    public float Orchestration = 0f;

    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;

    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;
    private EventInstance ambienceEventInstance;
    private EventInstance musicEventInstance;
    private Dictionary<FMOD.GUID, float> lastPlayedTimes = new();
    private float lastPlayedTime;

    private bool _loopEnded = false;
    private EVENT_CALLBACK _loopEndCallback;
    private string _markerName;
    private bool _printMarkerDebug = false;


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

    private void Update()
    {
        if (musicEventInstance.isValid() && _loopEnded)
        {
            _loopEnded = false;
            musicEventInstance.setParameterByName("Orchestration", Orchestration);
            Debug.Log($"[AudioManager] Loop ended, set Orchestration to {Orchestration}");
        }

        if (_printMarkerDebug)
        {
            Debug.Log($"[AudioManager] Reached marker: {_markerName}");
            _printMarkerDebug = false;
        }
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

    public static void PlayOneShot(EventReference eventReference, Vector3 position = new Vector3())
    {
        if (Instance == null || eventReference.IsNull) return;
        Instance.PlayOneShot(eventReference.Guid, position);
    }

    public static void PlayOneShot(string path, Vector3 position = new Vector3())
    {
        if (Instance == null) return;
        try
        {
            FMOD.GUID eventGuid = RuntimeManager.PathToGUID(path);
            Instance.PlayOneShot(eventGuid, position);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to play one-shot sound at path '{path}': {e.Message}");
        }
    }

    private void PlayOneShot(FMOD.GUID eventGuid, Vector3 position = new Vector3())
    {
        if (eventGuid.IsNull) return;

        lastPlayedTime = Time.time;
        lastPlayedTimes[eventGuid] = lastPlayedTime;
        RuntimeManager.PlayOneShot(eventGuid, position);
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

    public void PlayMusic(EventReference eventReference, bool useOrchestration = false)
    {
        musicEventInstance = CreateEventInstance(eventReference);
        musicEventInstance.start();
        if (useOrchestration)
        {
            AddOrchestrationCallback(musicEventInstance);
        }
    }

    private void AddOrchestrationCallback(EventInstance eventInstance)
    {
        _loopEndCallback = LoopEndCallback;
        eventInstance.setCallback(_loopEndCallback, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
    }

    private FMOD.RESULT LoopEndCallback(
        EVENT_CALLBACK_TYPE callbackType,
        IntPtr instancePtr,
        IntPtr propertyPtr
    )
    {
        // Now cast parameters to TIMELINE_MARKER_PROPERTIES
        var markerProps = (TIMELINE_MARKER_PROPERTIES)System.Runtime.InteropServices.Marshal.PtrToStructure(
            propertyPtr, typeof(TIMELINE_MARKER_PROPERTIES));

        _printMarkerDebug = true;
        _markerName = markerProps.name;
        // Check if the marker is called Loop
        if (markerProps.name == "Loop")
        {
            _loopEnded = true;
        }

        return FMOD.RESULT.OK;
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

    public bool PlayedRecently(EventReference eventReference, float cooldown)
    {
        return PlayedRecently(eventReference.Guid, cooldown);
    }

    public bool PlayedRecently(string path, float cooldown)
    {
        FMOD.GUID eventGuid = RuntimeManager.PathToGUID(path);
        return PlayedRecently(eventGuid, cooldown);
    }

    private bool PlayedRecently(FMOD.GUID eventGuid, float cooldown)
    {
        if (!lastPlayedTimes.ContainsKey(eventGuid))
            return false;

        float lastPlayedTime = lastPlayedTimes[eventGuid];
        return Time.time - lastPlayedTime < cooldown;
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