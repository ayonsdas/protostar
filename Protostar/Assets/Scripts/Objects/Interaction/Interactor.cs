using System.Collections.Generic;
using UnityEngine;

public abstract class Interactor : MonoBehaviour
{

    [SerializeField] private float range = 4f;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private Transform origin;

    protected readonly List<IInteractionCandidate> nearby = new();
    protected InteractionOption currentOption;
    protected IFocusable currentFocus;

    private readonly List<InteractionOption> _options = new();
    private readonly Dictionary<InteractionInputType, InteractionOption> _bestByInput
    = new Dictionary<InteractionInputType, InteractionOption>();

    protected abstract PlayerInteractionContext BuildContext();

    public virtual void Update()
    {
        ResolveInteraction();
    }

    // Find the most relevant interaction around us.
    private void ResolveInteraction()
    {
        var context = BuildContext();

        _options.Clear();

        foreach (var candidate in nearby)
        {
            candidate.CollectOptions(context, _options);
        }

        foreach (var option in _options)
        {
            if (!option.IsValid)
                continue;

            float score = CalculateScore(option);
            InteractionOption bestOption;
            if (_bestByInput.TryGetValue(option.InputType, out var currentBest))
            {
                if (score > currentBest.Score)
                {
                    _bestByInput[option.InputType] = option;
                }
            }
            else
            {
                _bestByInput.Add(option.InputType, option);
            }

            ApplyFocus(best);
        currentOption = best;
    }

    // Shift focus to new best InteractionOption
    private void ApplyFocus(InteractionOption interaction)
    {
        IFocusable newFocus = null;
        if (interaction.Source)
            newFocus = interaction.Source.gameObject.GetComponentInParent<IFocusable>();

        if (newFocus == currentFocus)
        {
            return;
        }

        currentFocus?.Unfocus(gameObject);
        newFocus?.Focus(gameObject);

        currentFocus = newFocus;
    }

    private float CalculateScore(InteractionOption option)
    {
        int basePriority = InteractionPriority.Get(option.Type);

        // Distance weighting
        var mb = option.Source;
        float distance = Vector3.Distance(transform.position, mb.transform.position);

        // Angle weighting
        Vector3 dir = (mb.transform.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, dir);

        return basePriority * 1000f + dot * 10f - distance;
    }
}
