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
    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;

    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;
    private EventInstance ambienceEventInstance;
    private EventInstance musicEventInstance;
    private Dictionary<FMOD.GUID, float> lastPlayedTimes = new();
    private float lastPlayedTime;

    public static AudioManager Instance { get; private set; }
    public static float SurfaceParameter = (float)SurfaceType.Default;

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

    public static void PlayOneShotOnSurface(
        EventReference eventReference,
        Vector3 position = new Vector3(),
        string surfaceParameterName = FMODParameters.FOOTSTEP_SURFACE_PARAMETER
    )
    {
        Dictionary<string, float> parameters = new()
        {
            [surfaceParameterName] = SurfaceParameter
        };

        //Debug.Log($"[AudioManager] playing event {eventReference.Path} on surface {SurfaceParameter}");
        PlayOneShot(eventReference, position, parameters);
    }

    public static void PlayOneShot(EventReference eventReference, Vector3 position = new Vector3(), Dictionary<string, float> parameters = null)
    {
        if (Instance == null || eventReference.IsNull) return;
        Instance.PlayOneShot(eventReference.Guid, position, parameters);
    }

    public static void PlayOneShot(string path, Vector3 position = new Vector3(), Dictionary<string, float> parameters = null)
    {
        if (Instance == null) return;
        try
        {
            FMOD.GUID eventGuid = RuntimeManager.PathToGUID(path);
            Instance.PlayOneShot(eventGuid, position, parameters);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to play one-shot sound at path '{path}': {e.Message}");
        }
    }

    private void PlayOneShot(FMOD.GUID eventGuid, Vector3 position = new Vector3(), Dictionary<string, float> parameters = null)
    {
        if (eventGuid.IsNull) return;

        lastPlayedTime = Time.time;
        lastPlayedTimes[eventGuid] = lastPlayedTime;

        if (CreateInstanceWithinMaxDistance(eventGuid, position, out EventInstance instance))
        {
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            SetParameters(instance, parameters);
            instance.start();
            instance.release();
        }
    }

    private static void SetParameters(EventInstance instance, Dictionary<string, float> parameters)
    {
        if (parameters == null || !instance.isValid()) return;

        foreach (var (name, value) in parameters)
        {
            instance.setParameterByName(name, value);
            // Debug.Log($"[AudioManager] Set parameter {name} to {value}");
        }
    }

    private static bool CreateInstanceWithinMaxDistance(FMOD.GUID guid, Vector3 position, out EventInstance instance)
    {
        EventDescription description = RuntimeManager.GetEventDescription(guid);
        if (Settings.Instance.StopEventsOutsideMaxDistance)
        {
            description.is3D(out bool is3D);
            if (is3D)
            {
                description.getMinMaxDistance(out float min, out float max);
                if (StudioListener.DistanceSquaredToNearestListener(position) > (max * max))
                {
                    instance = new EventInstance();
                    return false;
                }
            }
        }

        description.createInstance(out instance);
        return true;
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

    private FMOD.RESULT LoopEndCallback(
        EVENT_CALLBACK_TYPE callbackType,
        IntPtr instancePtr,
        IntPtr propertyPtr
    )
    {
        // Now cast parameters to TIMELINE_MARKER_PROPERTIES
        var markerProps = (TIMELINE_MARKER_PROPERTIES)System.Runtime.InteropServices.Marshal.PtrToStructure(
            propertyPtr, typeof(TIMELINE_MARKER_PROPERTIES));

        if (callbackType != EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
        {
            Debug.LogWarning($"[AudioManager] Received unexpected callback type: {callbackType}");
            return FMOD.RESULT.OK;
        }

        // Check if the marker is called Loop
        if (markerProps.name == "Loop")
        {

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