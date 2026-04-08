using System.Collections.Generic;
using UnityEngine;

public abstract class Interactor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] protected float interactionRadius = 3f;
    [SerializeField] protected LayerMask interactionMask;

    protected readonly List<IInteractionCandidate> nearby = new();
    protected IFocusable currentFocus;
    private IInteractionCandidate _focusedCandidate;
    protected IEngageable activeEngageable;
    protected IInteractionCandidate engagedCandidate;
    protected bool IsEngaged => activeEngageable != null && engagedCandidate != null;

    private readonly List<InteractionOption> _options = new();
    protected readonly Dictionary<InteractionInputType, InteractionOption> _bestOptionByInput
    = new Dictionary<InteractionInputType, InteractionOption>();

    protected abstract PlayerInteractionContext BuildContext();

    public virtual void Update()
    {
        ResolveInteraction();
    }

    // Find the most relevant interaction around us.
    private void ResolveInteraction()
    {
        IInteractionCandidate newFocus = null;
        var context = BuildContext();

        if (IsEngaged)
        {
            newFocus = engagedCandidate;
        }
        else
        {
            float bestFocusScore = float.MinValue;

            foreach (var candidate in nearby)
            {
                if (!candidate.HasAnyValidInteraction(context))
                    continue;

                float score = CalculateFocusScore(candidate, context);

                if (score > bestFocusScore)
                {
                    bestFocusScore = score;
                    newFocus = candidate;
                }
            }
        }

        ApplyFocus(newFocus);
        _focusedCandidate = newFocus;
        ResolveOptionsForFocused(context);
    }

    private void ResolveOptionsForFocused(PlayerInteractionContext context)
    {
        _bestOptionByInput.Clear();
        _options.Clear();

        if (_focusedCandidate == null)
            return;

        _focusedCandidate.CollectOptions(context, _options);

        foreach (var option in _options)
        {
            if (!option.IsValid)
                continue;

            float score = InteractionPriority.Get(option.Type);

            if (_bestOptionByInput.TryGetValue(option.InputType, out var existing))
            {
                float bestScore = InteractionPriority.Get(option.Type);
                if (score > bestScore)
                    _bestOptionByInput[option.InputType] = option;
            }
            else
            {
                _bestOptionByInput.Add(option.InputType, option);
            }
        }
    }

    // Shift focus to new best InteractionOption
    private void ApplyFocus(IInteractionCandidate candidate)
    {
        IFocusable newFocus = null;
        MonoBehaviour mb = candidate as MonoBehaviour;
        if (mb != null)
        {
            newFocus = mb.gameObject.GetComponentInParent<IFocusable>();
        }

        if (newFocus == currentFocus) return;

        var focusMB = currentFocus as MonoBehaviour;
        if (focusMB != null && focusMB.isActiveAndEnabled)
        {
            currentFocus?.Unfocus(gameObject);
        }

        if (mb != null && mb.isActiveAndEnabled)
        {
            newFocus?.Focus(gameObject);
        }

        currentFocus = newFocus;
    }

    private float CalculateFocusScore(IInteractionCandidate candidate, PlayerInteractionContext context)
    {

        var mb = candidate as MonoBehaviour;
        if (!mb)
            return float.MinValue;


        // Distance weighting
        float distance = Vector3.Distance(transform.position, mb.transform.position);
        float distanceWeight = Mathf.Clamp01(1f - (distance / interactionRadius));

        // Angle weighting
        Vector3 dir = (mb.transform.position - transform.position).normalized;
        float angleWeight = Vector3.Dot(transform.forward, dir) * 10f;

        return angleWeight + distanceWeight;
    }
}
