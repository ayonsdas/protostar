using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class CutsceneInteractionItem : MonoBehaviour, IInteractable, IInteractionCandidate
{
    [Header("Animation Settings")]
    public ImageCutscene Cutscene;
    [SerializeField] private Vector3 forwardAxis = Vector3.forward;
    [SerializeField] private InteractableOrbItem orbItem;

    [Header("Sound Settings")]
    [SerializeField] private EventReference pickupSound;
    private bool cutsceneTriggered = false;

    public void Interact(GameObject interactor)
    {
        StartCoroutine(InteractCoroutine(interactor));
    }

    public delegate void InteractAction(InteractItems item);
    public event InteractAction OnInteract; 
    public enum InteractItems
    {
        Item1,
        Item2,
        Item3
    }

    public InteractItems interactItem;

    private IEnumerator InteractCoroutine(GameObject interactor)
    {
        if (!interactor.CompareTag("Player"))
        {
            Debug.LogWarning($"[CutsceneInteractionItem] Interaction not from player, {interactor.name}");
            yield break;
        }

        if (orbItem != null)
        {
            // yield return StartCoroutine(orbItem.AbsorbOrbs(interactor.transform));
            StartCoroutine(orbItem.AbsorbOrbs(interactor.transform));
        }

        PickupAnimator animator = interactor.GetComponentInParent<PickupAnimator>();
        if (animator != null)
        {
            animator.PlayPickup(gameObject, forwardAxis, onComplete: PlayCutscene);
        }
        else
        {
            DefaultPickup();
            PlayCutscene();
        }
    }

    private void PlayCutscene()
    {
        if(OnInteract != null)
        {
            OnInteract(interactItem);
        }

        if (MenuManager.Instance == null)
        {
            Debug.LogWarning("[CutsceneInteractionItem] no menu manager, cant play cutscene");
            return;
        }

        MenuManager.Instance.PlayCutscene(Cutscene);
        MenuManager.Instance.AddCutsceneCloseCallback(Cutscene, HandleClose);
    }

    private void HandleClose()
    {
        cutsceneTriggered = true;
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
