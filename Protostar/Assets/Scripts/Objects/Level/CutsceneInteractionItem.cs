using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class CutsceneInteractionItem : MonoBehaviour, IInteractable, IInteractionCandidate
{
    public ManualCutscene Cutscene;
    [Header("Sound Settings")]
    [SerializeField] private EventReference pickupSound;
    private bool cutsceneTriggered = false;
    public void Interact(GameObject interactor)
    {
        PickupAnimator animator = interactor.GetComponentInParent<PickupAnimator>();
        if (animator != null)
        {
            animator.PlayPickup(gameObject, onComplete: Cutscene.Play);
        }
        else
        {
            DefaultPickup();
            Cutscene.Play();
        }
    }

    private void DefaultPickup()
    {
        AudioManager.PlayOneShot(pickupSound);
        gameObject.SetActive(false);
    }

    public void CollectOptions(PlayerInteractionContext context, List<InteractionOption> options)
    {
        if (!cutsceneTriggered)
        {
            options.Add(InteractionOptionBuilder.Create(
                InteractionType.Interact,
                this
            ));
        }
    }
}
