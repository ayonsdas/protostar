using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class PianoKey : MonoBehaviour, IAudioSurface
{
    private const string PIANO_KEY_PARAMETER = "PianoKey";
    [SerializeField] private int _keyValue;
    [SerializeField] private EventReference pianoEvent;

    public void Play(Vector3 playerPosition)
    {
        Debug.Log("Playing piano key");
        if (AudioManager.Instance == null || pianoEvent.IsNull) return;

        Dictionary<string, float> parameters = new()
        {
            [PIANO_KEY_PARAMETER] = _keyValue
        };
        Debug.Log($"Piano key event {pianoEvent.Path} parameter {PIANO_KEY_PARAMETER} = {_keyValue}");

        AudioManager.PlayOneShot(pianoEvent, playerPosition, parameters);
    }
}