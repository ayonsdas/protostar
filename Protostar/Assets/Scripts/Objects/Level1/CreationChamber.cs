using System.Collections.Generic;
using UnityEngine;

public class CreationChamber : MonoBehaviour, IInteractable, IInteractionCandidate
{
    public void Interact(GameObject interactor)
    {
        Debug.Log($"[CreationChamber] Interacting!");
    }

    public void CollectOptions(PlayerInteractionContext context, List<InteractionOption> options)
    {
        options.Add(InteractionOptionBuilder.Create(
            InteractionType.Interact,
            this
        ));
    }
}
