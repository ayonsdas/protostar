using UnityEngine;

public class InteractionOrbs : MonoBehaviour
{
    [SerializeField] private BaseInteractable interactable;
    [SerializeField] private OrbVFX orbVFX;
    private Transform player;

    private bool absorbed = false;

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.OnInteracted += HandleInteraction;
            return;
        }
        Debug.LogError("[InteractionOrbs] null interactable, needs to be assigned");
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.OnInteracted -= HandleInteraction;
            return;
        }
        Debug.LogError("[InteractionOrbs] null interactable, needs to be assigned");
    }

    private void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void HandleInteraction()
    {
        if (absorbed || player == null) return;

        absorbed = true;
        StartCoroutine(orbVFX.AbsorbOrbs());
    }
}
