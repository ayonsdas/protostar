using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CustomGravityBody))]
public class BookObject : MonoBehaviour, IPickupable, IPlaceable, IInteractionCandidate
{
    private CustomGravityBody gravityBody;
    private Rigidbody rb;
    private Collider[] physicsColliders;
    private bool isPickedUp = false;
    private GameObject currentPicker = null;
    private bool isInSlot = false;
    private bool _engaged = false;

    private bool IsHeld => isPickedUp;
    public bool IsInSlot => isInSlot;
    private bool IsFree => !isPickedUp && !isInSlot;

    private void Awake()
    {
        gravityBody = GetComponent<CustomGravityBody>();
        rb = GetComponent<Rigidbody>();

        // Get only non-trigger colliders for physics
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        System.Collections.Generic.List<Collider> physicsList = new System.Collections.Generic.List<Collider>();
        foreach (var col in allColliders)
        {
            if (!col.isTrigger)
            {
                physicsList.Add(col);
            }
        }
        physicsColliders = physicsList.ToArray();

        // Set initial gravity to the seed's local down direction (-Y axis)
        SetGravityDirection(-transform.up);
    }

    private void Update()
    {
        // While being held, continuously sync with player's gravity
        if (isPickedUp && currentPicker != null)
        {
            var playerGravityBody = currentPicker.GetComponent<CustomGravityBody>();
            if (playerGravityBody != null)
            {
                SetGravityDirection(playerGravityBody.GetGravityDirection());
            }
        }
    }

    public bool CanPickup()
    {
        return !isPickedUp && !isInSlot;
    }

    /// <summary>
    /// Set the gravity direction for this book
    /// </summary>
    public void SetGravityDirection(Vector3 direction)
    {
        if (gravityBody != null)
        {
            gravityBody.SetCustomGravityDirection(direction.normalized);
        }
    }

    public void OnPickup(GameObject picker)
    {
        isPickedUp = true;
        currentPicker = picker;

        // Make kinematic while held
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Disable colliders while held
        foreach (var col in physicsColliders)
        {
            col.enabled = false;
        }

        // Gravity will be continuously synced in Update()
    }

    public void OnDrop(GameObject picker)
    {
        isPickedUp = false;
        currentPicker = null;

        // Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Re-enable colliders
        foreach (var col in physicsColliders)
        {
            col.enabled = true;
        }

        // Maintain current gravity direction (player's gravity at drop time)
        // No need to change anything - gravityBody already has the correct direction
    }

    // --- Slot placement ---
    /// <summary>
    /// Called when this book is placed into a Slot.
    /// Keeps the book kinematic with colliders off.
    /// </summary>
    public void OnPlace()
    {
        isInSlot = true;
        isPickedUp = false;
        currentPicker = null;

        if (rb != null) rb.isKinematic = true;

        foreach (var col in physicsColliders)
            col.enabled = false;

        Debug.Log($"[BookObject] {gameObject.name} placed in slot.");
    }

    /// <summary>
    /// Called when this book is removed from a Slot.
    /// Re-enables physics so it can be picked up again.
    /// </summary>
    public void OnRemove()
    {
        isInSlot = false;

        if (rb != null) rb.isKinematic = false;

        foreach (var col in physicsColliders)
            col.enabled = true;

        Debug.Log($"[BookObject] {gameObject.name} removed from slot.");
    }
    
    public void CollectOptions(PlayerInteractionContext context, List<InteractionOption> options)
    {
        // If free on ground then pickup
        if (IsFree)
        {
            options.Add(InteractionBuilder.Create(
                InteractionType.Pickup,
                this
            ));
        }

        // If currently held by this interactor then can drop
        if (IsHeld && gameObject.Equals(context.CarriedObject))
        {
            options.Add(InteractionBuilder.Create(
                InteractionType.Drop,
                this
            ));
        }
    }
}
