using UnityEngine;

public class Interactor : MonoBehaviour
{

    [SerializeField] private float range = 4f;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private Transform origin;

    public IEngageable Engaged;
    public IInteractable HoveredInteractable;
    public IEngageable HoveredEngagable;
    public IFocusable Focused;
    public IPickupable HoveredPickupable;

    public virtual void Update()
    {
        Cast();
    }

    private void Cast()
    {
        IInteractable newHoveredInteractable = null;
        IEngageable newHoveredEngagable = null;
        IFocusable newFocused = null;
        IPickupable newHoveredPickupable = null;


        if (Physics.Raycast(origin.position, origin.forward, out var hit, range, interactableMask))
        {
            newHoveredInteractable = hit.collider.GetComponentInParent<IInteractable>();
            newHoveredEngagable = hit.collider.GetComponentInParent<IEngageable>();
            newFocused = hit.collider.GetComponentInParent<IFocusable>();
            newHoveredPickupable = hit.collider.GetComponentInParent<IPickupable>();
        }

        if (newFocused != Focused)
        {
            Focused?.Unfocus(gameObject);
            newFocused?.Focus(gameObject);
            Focused = newFocused;
        }

        HoveredInteractable = newHoveredInteractable;
        HoveredEngagable = newHoveredEngagable;
        HoveredPickupable = newHoveredPickupable;
    }


    public void Interact()
    {
        if (Engaged != null)
        {
            Engaged.Disengage(gameObject);
            Engaged = null;
            return;
        }

        if (HoveredEngagable != null)
        {
            HoveredEngagable.Engage(gameObject);
            Engaged = HoveredEngagable;
        }

        HoveredInteractable?.Interact(gameObject);
    }

    public void Scroll(int direction)
    {
        if (Engaged is IShiftable shiftable)
        {
            shiftable.Shift(direction);
        }
    }
}
