using System.Collections.Generic;
using UnityEngine;

public class CutsceneInteractionItem : MonoBehaviour, IInteractable, IInteractionCandidate
{
    [SerializeField] private ManualCutscene cutscene;
    private bool cutsceneTriggered = false;
    public void Interact(GameObject interactor)
    {
        Debug.Log($"[CutsceneInteractionItem] Interacting!");
        cutscene.cutsceneCanvas.enabled = true;
        GameStateManager.Instance.SetState(GameState.Cutscene);
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
