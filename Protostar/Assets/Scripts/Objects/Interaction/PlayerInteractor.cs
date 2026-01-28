using UnityEngine.InputSystem;
using UnityEngine;

public class PlayerInteractor : Interactor
{
    [Header("Pickup Settings")]
    [SerializeField] private Transform pickupHoldPoint; // Position above player's head
    [SerializeField] private float dropDistance = 2f; // Distance in front to drop

    private GameObject carriedObject;
    private IPickupable carriedPickupable;

    public void OnInteract(InputValue value)
    {
        // Only allow interact if not carrying something
        if (value.isPressed && carriedObject == null)
        {
            Debug.Log("Interacted");
            Interact();
        }
    }

    public void OnShift(InputValue value)
    {
        // Only allow shifting if not carrying something
        if (carriedObject == null)
        {
            int direction = value.Get<float>() > 0 ? 1 : -1;
            Scroll(direction);
        }
    }

    public void OnPickup(InputValue value)
    {
        Debug.Log($"OnPickup called - isPressed: {value.isPressed}, carriedObject: {(carriedObject != null ? carriedObject.name : "null")}");

        if (!value.isPressed) return;

        if (carriedObject != null)
        {
            // Drop the object
            DropObject();
        }
        else
        {
            // Try to pick up object
            TryPickupObject();
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