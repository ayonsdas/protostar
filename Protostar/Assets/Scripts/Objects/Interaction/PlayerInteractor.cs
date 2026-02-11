using UnityEngine.InputSystem;
using UnityEngine;

public class PlayerInteractor : Interactor
{
    [Header("Pickup Settings")]
    [SerializeField] private Transform pickupHoldPoint; // Position above player's head
    [SerializeField] private float dropDistance = 2f; // Distance in front to drop

    private GameObject carriedObject;
    private IPickupable carriedPickupable;

    // Shift state
    private bool isShiftHeld = false;
    private IEngageable shiftEngaged = null;
    private IShiftable shiftTarget = null;
    private Vector2 shiftInput = Vector2.zero; // WASD input accumulated during shift hold
    private PlayerController playerController;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;
        if (isShiftHeld) return; // Don't interact while shifting

        // Priority 1: If carrying something, drop it
        if (carriedObject != null)
        {
            Debug.Log("[PlayerInteractor] OnInteract - dropping carried object");
            DropObject();
            return;
        }

        // Priority 2: If hovering a pickupable, pick it up
        if (HoveredPickupable != null)
        {
            Debug.Log("[PlayerInteractor] OnInteract - picking up object");
            TryPickupObject();
            return;
        }

        // Priority 3: Normal interact (telescope, cabinet, etc.)
        Debug.Log("[PlayerInteractor] OnInteract - calling Interact()");
        Interact();
    }

    public void OnShift(InputValue value)
    {
        if (value.isPressed)
        {
            // Shift pressed - try to engage with hovered shiftable
            if (carriedObject != null) return; // Can't shift while carrying

            Cast(); // Make sure we have latest raycast
            
            // Look for an engageable+shiftable object
            IEngageable engageable = HoveredEngagable;
            IShiftable shiftable = null;
            if (engageable != null)
            {
                shiftable = (engageable as MonoBehaviour)?.GetComponent<IShiftable>();
            }

            if (engageable != null && shiftable != null)
            {
                isShiftHeld = true;
                shiftEngaged = engageable;
                shiftTarget = shiftable;
                shiftInput = Vector2.zero;

                // Engage the object
                engageable.Engage(gameObject);

                // Lock player movement
                if (playerController != null)
                {
                    playerController.SetMovementLocked(true);
                }

                Debug.Log($"[PlayerInteractor] Shift engaged with {(engageable as MonoBehaviour)?.gameObject.name}");
            }
        }
        else
        {
            // Shift released - apply shift and disengage
            if (isShiftHeld && shiftTarget != null)
            {
                // Determine shift direction from accumulated WASD input
                // W/D = forward (+1), S/A = backward (-1), no input = forward (+1) default
                int direction = 1;
                if (Mathf.Abs(shiftInput.y) > Mathf.Abs(shiftInput.x))
                {
                    direction = shiftInput.y >= 0 ? 1 : -1;
                }
                else if (Mathf.Abs(shiftInput.x) > 0.01f)
                {
                    direction = shiftInput.x >= 0 ? 1 : -1;
                }

                Debug.Log($"[PlayerInteractor] Shift released, direction={direction}, input=({shiftInput.x},{shiftInput.y})");
                shiftTarget.Shift(direction);

                // Disengage
                shiftEngaged?.Disengage(gameObject);
            }

            // Clean up
            isShiftHeld = false;
            shiftEngaged = null;
            shiftTarget = null;
            shiftInput = Vector2.zero;

            // Unlock player movement
            if (playerController != null)
            {
                playerController.SetMovementLocked(false);
            }
        }
    }

    public void OnMove(InputValue value)
    {
        // When shift is held, capture WASD as shift input instead of movement
        if (isShiftHeld)
        {
            shiftInput = value.Get<Vector2>();
        }
    }

    private void TryPickupObject()
    {
        Debug.Log($"TryPickupObject - HoveredPickupable: {(HoveredPickupable != null ? "found" : "null")}");

        IPickupable pickupable = HoveredPickupable;

        if (pickupable != null)
        {
            carriedObject = (pickupable as MonoBehaviour).gameObject;
            carriedPickupable = pickupable;

            Debug.Log($"Picking up {carriedObject.name}");

            // Parent to hold point
            carriedObject.transform.SetParent(pickupHoldPoint);
            carriedObject.transform.localPosition = Vector3.zero;
            carriedObject.transform.localRotation = Quaternion.identity;

            pickupable.OnPickup(gameObject);
        }
        else
        {
            Debug.Log("No pickupable object found to pick up");
        }
    }

    private void DropObject()
    {
        if (carriedObject == null) return;

        // Calculate drop position in front of player in local space
        Vector3 dropPosition = transform.position + transform.forward * dropDistance;

        // Unparent and place
        carriedObject.transform.SetParent(null);
        carriedObject.transform.position = dropPosition;

        // Notify the object
        carriedPickupable?.OnDrop(gameObject);

        carriedObject = null;
        carriedPickupable = null;
    }
}