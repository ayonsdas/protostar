using UnityEngine;
using FMODUnity;

public class SceneAudioSetup : MonoBehaviour
{
    [SerializeField] private EventReference musicEventReference;
    [SerializeField] private EventReference ambienceEventReference;
    [SerializeField] private bool useOrchestration = false;
    private void Start()
    {
        if (!musicEventReference.IsNull)
        {
            AudioManager.Instance.PlayMusic(musicEventReference, useOrchestration);
        }

        if (!ambienceEventReference.IsNull)
        {
            AudioManager.Instance.PlayAmbience(ambienceEventReference);
        }
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance)
        {
            AudioManager.Instance.SetMusicActive(false);
            AudioManager.Instance.SetAmbienceActive(false);
        }
    }

    private void OnEnable()
    {
        if (GameStateManager.Instance)
            GameStateManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance)
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        // switch (state)
        // {
        //     case GameState.InGame:
        //     case GameState.Cutscene:
        //         AudioManager.Instance.SetMusicActive(true);
        //         break;
        //     default:
        //         AudioManager.Instance.SetMusicActive(false);
        //         break;
        // }
    }
}
