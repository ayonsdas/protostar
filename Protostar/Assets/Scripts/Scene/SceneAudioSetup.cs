using UnityEngine;
using FMODUnity;

public class SceneAudioSetup : MonoBehaviour
{
    [SerializeField] private EventReference musicEventReference;
    [SerializeField] private EventReference ambienceEventReference;

    private void Start()
    {
        InitializeSceneSound();
    }

    private void OnDestroy()
    {
        DisableSceneSound();
    }

    private void InitializeSceneSound()
    {
        if (AudioManager.Instance == null) return;

        try
        {
            if (!musicEventReference.IsNull)
            {
                AudioManager.Instance.PlayMusic(musicEventReference);
            }

            if (!ambienceEventReference.IsNull)
            {
                AudioManager.Instance.PlayAmbience(ambienceEventReference);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SceneAudioSetup] Failed to start scene audio: {e.Message}");
        }
    }

    private void DisableSceneSound()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.SetMusicActive(false);
        AudioManager.Instance.SetAmbienceActive(false);
    }
}
