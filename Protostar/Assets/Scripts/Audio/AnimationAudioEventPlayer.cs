using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class AnimationAudioEventPlayer : MonoBehaviour
{
    [Tooltip("List of other audio events to check cooldowns for when playing an event")]
    [SerializeField] private List<EventReference> cooldownEventReferences = new();

    private Dictionary<string, float> eventCooldowns = new();
    private bool CanPlayEvent(string path)
    {
        // If no know cooldown, then assume can play
        if (!eventCooldowns.ContainsKey(path))
            return true;

        // Check if same event was played recently
        float cooldownTime = eventCooldowns[path];
        if (AudioManager.Instance.PlayedRecently(path, cooldownTime))
        {
            return false;
        }

        // Check if any of the cooldown events were played recently
        foreach (EventReference eventReference in cooldownEventReferences)
        {
            if (AudioManager.Instance.PlayedRecently(eventReference, cooldownTime))
            {
                return false;
            }
        }

        return true;
    }

    public void PlayOneShot(AnimationEvent animationEvent)
    {
        string eventName = animationEvent.stringParameter;
        float cooldownTime = animationEvent.floatParameter;
        eventCooldowns[eventName] = cooldownTime;

        if (!CanPlayEvent(eventName))
            return;

        AudioManager.Instance.PlayOneShot(eventName, transform.position);
    }
}
