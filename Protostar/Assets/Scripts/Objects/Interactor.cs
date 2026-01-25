using UnityEngine;

public class Interactor : MonoBehaviour
{

    [SerializeField] private float range = 4f;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private Transform origin;

    public IEngageable Engaged;
    public IInteractable Hovered;
    public IFocusable Focused;

    void Update()
    {
        Cast();
    }

    void Cast()
    {
        IInteractable newHovered = null;
        IFocusable newFocused = null;


        if (Physics.Raycast(origin.position, origin.forward, out var hit, range, interactableMask))
        {
            newHovered = hit.collider.GetComponentInParent<IInteractable>();
            newFocused = hit.collider.GetComponentInParent<IFocusable>();
        }

        if (newFocused != Focused)
        {
            Focused.Unfocus(gameObject);
            newFocused.Focus(gameObject);
            Focused = newFocused;
        }
    }


    public void Interact()
    {
        // Engage or disengage
        if (Engaged != null)
        {
            Engaged.Disengage(gameObject);
            Engaged = null;
            return;
        }

        if (Hovered is IEngageable engageable)
        {
            engageable.Engage(gameObject);
            Engaged = engageable;
        }
        else
        {
            Hovered?.Interact(gameObject);
        }
    }

    public void Scroll(int direction)
    {
        if (Engaged is IShiftable shiftable)
        {
            shiftable.Shift(direction);
        }
    }
}
