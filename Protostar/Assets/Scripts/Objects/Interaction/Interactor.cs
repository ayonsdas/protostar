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

    protected void Cast()
    {
        IInteractable newHoveredInteractable = null;
        IEngageable newHoveredEngagable = null;
        IFocusable newFocused = null;
        IPickupable newHoveredPickupable = null;


        if (Physics.Raycast(origin.position, origin.forward, out var hit, range, interactableMask, QueryTriggerInteraction.Collide))
        {
            newHoveredInteractable = hit.collider.GetComponentInParent<IInteractable>();
            newHoveredEngagable = hit.collider.GetComponentInParent<IEngageable>();
            newFocused = hit.collider.GetComponentInParent<IFocusable>();
            newHoveredPickupable = hit.collider.GetComponentInParent<IPickupable>();
            
            // Filter out pickupables that can't currently be picked up
            if (newHoveredPickupable != null && !newHoveredPickupable.CanPickup())
            {
                newHoveredPickupable = null;
            }
        }
        else
        {
            // Debug: do an unfiltered raycast to see if we're hitting something on the wrong layer
            if (Physics.Raycast(origin.position, origin.forward, out var debugHit, range))
            {
                Debug.Log($"[Interactor] Ray hit '{debugHit.collider.gameObject.name}' on layer {debugHit.collider.gameObject.layer} ({LayerMask.LayerToName(debugHit.collider.gameObject.layer)}), but NOT on interactableMask. Check the layer!");
            }
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
        // Make sure we have the latest raycast data
        Cast();
        
        Debug.Log($"[Interactor] Interact() called. Engaged={Engaged != null}, HoveredEngagable={HoveredEngagable != null}, HoveredInteractable={HoveredInteractable != null}");
        
        if (Engaged != null)
        {
            Debug.Log("[Interactor] Disengaging");
            Engaged.Disengage(gameObject);
            Engaged = null;
            return;
        }

        if (HoveredEngagable != null)
        {
            Debug.Log("[Interactor] Engaging HoveredEngagable");
            HoveredEngagable.Engage(gameObject);
            Engaged = HoveredEngagable;
        }

        Debug.Log($"[Interactor] About to call HoveredInteractable.Interact, HoveredInteractable is null: {HoveredInteractable == null}");
        if (HoveredInteractable != null)
        {
            Debug.Log($"[Interactor] HoveredInteractable type: {HoveredInteractable.GetType().Name}");
        }
        HoveredInteractable?.Interact(gameObject);
        Debug.Log("[Interactor] After calling HoveredInteractable.Interact");
    }

    public void Scroll(int direction)
    {
        if (Engaged is IShiftable shiftable)
        {
            shiftable.Shift(direction);
        }
    }
}
