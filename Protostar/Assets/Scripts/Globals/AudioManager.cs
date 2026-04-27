using System;
using System.Collections.Generic;
using System.Linq;
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
        try
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
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to set bus volume: {e.Message}");
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
        try
        {
            Instance.PlayOneShot(eventReference.Guid, position, parameters);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to play one-shot sound: {e.Message}");
        }
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

        try
        {
            lastPlayedTime = Time.time;
            lastPlayedTimes[eventGuid] = lastPlayedTime;

            string parameterString = "";
            if (parameters != null)
                parameterString = string.Join(", ", parameters.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

            if (CreateInstanceWithinMaxDistance(eventGuid, position, out EventInstance instance))
            {
                instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
                SetParameters(instance, parameters);
                instance.start();
                instance.release();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to play one-shot FMOD event: {e.Message}");
        }
    }

    private static void SetParameters(EventInstance instance, Dictionary<string, float> parameters)
    {
        if (parameters == null || !instance.isValid()) return;

        try
        {
            foreach (var (name, value) in parameters)
            {
                instance.setParameterByName(name, value);
                // Debug.Log($"[AudioManager] Set parameter {name} to {value}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to set audio parameters: {e.Message}");
        }
    }

    private static bool CreateInstanceWithinMaxDistance(FMOD.GUID guid, Vector3 position, out EventInstance instance)
    {
        try
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
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to create event instance: {e.Message}");
            instance = new EventInstance();
            return false;
        }
    }

    public EventInstance CreateEventInstance(EventReference eventReference)
    {
        try
        {
            EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
            eventInstances.Add(eventInstance);
            return eventInstance;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to create event instance: {e.Message}");
            return new EventInstance();
        }
    }

    public EventInstance CreateEventInstance(EventReference eventReference, Vector3 position)
    {
        try
        {
            EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
            eventInstances.Add(eventInstance);
            eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            return eventInstance;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to create event instance with position: {e.Message}");
            return new EventInstance();
        }
    }

    public StudioEventEmitter InitializeEventEmitter(EventReference eventReference, GameObject emitter)
    {
        try
        {
            StudioEventEmitter eventEmitter = emitter.GetComponent<StudioEventEmitter>();
            if (eventEmitter == null)
            {
                Debug.LogWarning($"[AudioManager] No StudioEventEmitter found on {emitter.name}");
                return null;
            }
            eventEmitter.EventReference = eventReference;
            eventEmitters.Add(eventEmitter);
            return eventEmitter;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to initialize event emitter: {e.Message}");
            return null;
        }
    }

    public void PlayMusic(EventReference eventReference)
    {
        try
        {
            musicEventInstance = CreateEventInstance(eventReference);
            if (musicEventInstance.isValid())
            {
                musicEventInstance.start();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to play music: {e.Message}");
        }
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
        try
        {
            if (!musicEventInstance.isValid()) return;
            
            if (active)
            {
                musicEventInstance.start();
            }
            else
            {
                musicEventInstance.stop(stopMode);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to set music active state: {e.Message}");
        }
    }

    public void SetMusicParameter(string parameterName, float value)
    {
        try
        {
            if (!musicEventInstance.isValid()) return;
            
            musicEventInstance.setParameterByName(parameterName, value);
            Debug.Log($"[AudioManager] Set music parameter {parameterName} to {value}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to set music parameter: {e.Message}");
        }
    }

    public void PlayAmbience(EventReference eventReference)
    {
        try
        {
            ambienceEventInstance = CreateEventInstance(eventReference);
            if (ambienceEventInstance.isValid())
            {
                ambienceEventInstance.start();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to play ambience: {e.Message}");
        }
    }

    public void SetAmbienceActive(bool active, FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.IMMEDIATE)
    {
        try
        {
            if (!ambienceEventInstance.isValid()) return;
            
            if (active)
            {
                ambienceEventInstance.start();
            }
            else
            {
                ambienceEventInstance.stop(stopMode);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to set ambience active state: {e.Message}");
        }
    }

    public bool PlayedRecently(EventReference eventReference, float cooldown)
    {
        return PlayedRecently(eventReference.Guid, cooldown);
    }

    public bool PlayedRecently(string path, float cooldown)
    {
        try
        {
            FMOD.GUID eventGuid = RuntimeManager.PathToGUID(path);
            return PlayedRecently(eventGuid, cooldown);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Failed to check if played recently: {e.Message}");
            return false;
        }
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
        try
        {
            if (eventInstances != null)
            {
                foreach (EventInstance eventInstance in eventInstances)
                {
                    if (eventInstance.isValid())
                    {
                        eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                        eventInstance.release();
                    }
                }
            }
            if (eventEmitters != null)
            {
                foreach (StudioEventEmitter emitter in eventEmitters)
                {
                    if (emitter != null)
                    {
                        emitter.Stop();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] Error during audio cleanup: {e.Message}");
        }
    }


    private void OnDestroy()
    {
        Cleanup();
    }
}