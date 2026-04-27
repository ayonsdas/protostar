using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using UnityEngine;

public class AnimationAudioEventPlayer : MonoBehaviour
{
    [Tooltip("List of other audio events to check cooldowns for when playing an event")]
    [SerializeField] private List<EventReference> cooldownEventReferences = new();

    private Dictionary<EventReference, float> eventCooldowns = new();
    private bool CanPlayEvent(EventReference eventReference)
    {
        // If no known cooldown, then assume can play
        if (!eventCooldowns.ContainsKey(eventReference))
            return true;

        // Check if same event was played recently
        float cooldownTime = eventCooldowns[eventReference];
        if (AudioManager.Instance.PlayedRecently(eventReference, cooldownTime))
        {
            return false;
        }

        // Check if any of the cooldown events were played recently
        foreach (EventReference cooldownReference in cooldownEventReferences)
        {
            if (AudioManager.Instance.PlayedRecently(cooldownReference, cooldownTime))
            {
                return false;
            }
        }

        return true;
    }

    public void PlayOneShot(AnimationEvent animationEvent)
    {
        var soundEvent = animationEvent.objectReferenceParameter as SoundEvent;
        if (soundEvent == null) return;

        eventCooldowns[soundEvent.Event] = soundEvent.Cooldown;

        if (!CanPlayEvent(soundEvent.Event))
            return;

        AudioManager.PlayOneShotOnSurface(soundEvent.Event, transform.position, soundEvent.SurfaceParameterName);
    }
}
