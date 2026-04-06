using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class CutsceneInteractionItem : MonoBehaviour, IInteractable, IInteractionCandidate
{
    [SerializeField] private ManualCutscene cutscene;
    [Header("Sound Settings")]
    [SerializeField] private EventReference pickupSound;
    private bool cutsceneTriggered = false;
    public void Interact(GameObject interactor)
    {
        PickupAnimator animator = interactor.GetComponentInParent<PickupAnimator>();
        if (animator != null)
        {
            animator.PlayPickup(gameObject, onComplete: PlayCutscene);
        }
        else
        {
            DefaultPickup();
            PlayCutscene();
        }
    }

    private void PlayCutscene()
    {
        cutscene.cutsceneCanvas.enabled = true;
        GameStateManager.Instance.SetState(GameState.Cutscene);
    }

    private void PickupObject(GameObject interactor)
    {

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
