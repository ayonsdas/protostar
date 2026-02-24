using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Seed object that can be picked up and has custom gravity.
/// When dropped, maintains the player's gravity direction at time of drop.
/// Can be shifted (Left Shift) to grow into a tree the player can jump on.
/// </summary>
[RequireComponent(typeof(CustomGravityBody))]
public class SeedObject : MonoBehaviour, IPickupable, IPlaceable, IEngageable, IShiftable, IInteractionCandidate
{
    [Header("Colliders")]
    [SerializeField] private Collider pickupTrigger; // Optional: separate trigger collider for easier pickup

    [Header("Tree Shift")]
    [SerializeField] private GameObject treeModel; // Assign a tree prefab/model as a child (disabled by default)
    [SerializeField] private GameObject seedModel; // The seed visual (if separate from root)

    private CustomGravityBody gravityBody;
    private Rigidbody rb;
    private Collider[] physicsColliders;
    private bool isPickedUp = false;
    private GameObject currentPicker = null;
    private bool isShifted = false;
    private bool isInSlot = false;
    private bool _engaged = false;

    private bool IsHeld => isPickedUp;
    public bool IsInSlot => isInSlot;
    private bool IsShiftable => !isPickedUp && !isInSlot;
    private bool IsFree => !isPickedUp && !isInSlot && !isShifted;

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

        // Make sure tree model is hidden at start
        if (treeModel != null)
        {
            treeModel.SetActive(false);
        }
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

    /// <summary>
    /// Set the gravity direction for this seed
    /// </summary>
    public void SetGravityDirection(Vector3 direction)
    {
        if (gravityBody != null)
        {
            gravityBody.SetCustomGravityDirection(direction.normalized);
        }
    }

    /// <summary>
    /// Get the current gravity direction for this seed
    /// </summary>
    public Vector3 GetGravityDirection()
    {
        if (gravityBody != null)
        {
            return gravityBody.GetGravityDirection();
        }
        return Vector3.down;
    }

    public bool CanPickup()
    {
        return !isShifted && !isPickedUp && !isInSlot;
    }

    public void OnPickup(GameObject picker)
    {
        // Can't pick up when shifted into tree form
        if (isShifted) return;

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

        // Also disable pickup trigger if assigned
        if (pickupTrigger != null)
        {
            pickupTrigger.enabled = false;
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

        // Also re-enable pickup trigger if assigned
        if (pickupTrigger != null)
        {
            pickupTrigger.enabled = true;
        }

        // Maintain current gravity direction (player's gravity at drop time)
        // No need to change anything - gravityBody already has the correct direction
    }

    // --- Slot placement ---

    /// <summary>
    /// Called when this seed is placed into a SeedSlot.
    /// Keeps the seed kinematic with colliders off.
    /// </summary>
    public void OnPlace()
    {
        isInSlot = true;
        isPickedUp = false;
        currentPicker = null;

        if (rb != null) rb.isKinematic = true;

        foreach (var col in physicsColliders)
            col.enabled = false;

        if (pickupTrigger != null)
            pickupTrigger.enabled = false;

        Debug.Log($"[SeedObject] {gameObject.name} placed in slot.");
    }

    /// <summary>
    /// Called when this seed is removed from a SeedSlot.
    /// Re-enables physics so it can be picked up again.
    /// </summary>
    public void OnRemove()
    {
        isInSlot = false;

        if (rb != null) rb.isKinematic = false;

        foreach (var col in physicsColliders)
            col.enabled = true;

        if (pickupTrigger != null)
            pickupTrigger.enabled = true;

        Debug.Log($"[SeedObject] {gameObject.name} removed from slot.");
    }

    // --- IEngageable ---
    public void Engage(GameObject interactor)
    {
        _engaged = true;
        Debug.Log($"[SeedObject] Engaged with {gameObject.name}. isShifted={isShifted}");
    }

    public void Disengage(GameObject interactor)
    {
        _engaged = false;
        Debug.Log($"[SeedObject] Disengaged from {gameObject.name}");
    }

    // --- IShiftable ---
    public void Shift(int direction)
    {
        if (isPickedUp) return;

        if (!isShifted)
        {
            ShiftToTree();
        }
        else
        {
            ShiftToSeed();
        }
    }

    private void ShiftToTree()
    {
        isShifted = true;
        Debug.Log($"[SeedObject] Shifting {gameObject.name} into tree!");

        // Hide seed visual - disable renderer instead of GameObject
        if (seedModel != null)
        {
            Renderer[] renderers = seedModel.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;
        }

        // Show tree model
        if (treeModel != null)
        {
            treeModel.SetActive(true);
            // Ensure tree and all children are on the same layer as the seed (Interactable)
            SetLayerRecursive(treeModel, gameObject.layer);
        }

        // Make it static and solid so the player can stand on it
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Disable pickup trigger so it can't be picked up as a tree
        if (pickupTrigger != null)
        {
            pickupTrigger.enabled = false;
        }

        // Disable the seed's small colliders
        foreach (var col in physicsColliders)
        {
            col.enabled = false;
        }

        Debug.Log($"[SeedObject] {gameObject.name} is now a tree!");
    }

    private void ShiftToSeed()
    {
        isShifted = false;
        Debug.Log($"[SeedObject] Shifting {gameObject.name} back to seed!");

        // Show seed visual
        if (seedModel != null)
        {
            Renderer[] renderers = seedModel.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = true;
        }

        // Hide tree model
        if (treeModel != null)
        {
            treeModel.SetActive(false);
        }

        // Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Re-enable pickup trigger
        if (pickupTrigger != null)
        {
            pickupTrigger.enabled = true;
        }

        // Re-enable the seed's colliders
        foreach (var col in physicsColliders)
        {
            col.enabled = true;
        }

        Debug.Log($"[SeedObject] {gameObject.name} is a seed again!");
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    public void CollectOptions(PlayerInteractionContext context, List<InteractionOption> options)
    {
        if (IsShiftable)
        {
            options.Add(InteractionOptionBuilder.Create(
                InteractionType.Shift,
                this
            ));
        }

        // If free on ground then pickup
        if (IsFree)
        {
            options.Add(InteractionOptionBuilder.Create(
                InteractionType.Pickup,
                this
            ));
        }

        // If currently held by this interactor then can drop
        if (IsHeld)
        {
            options.Add(InteractionOptionBuilder.Create(
                InteractionType.Drop,
                this
            ));
        }
    }
}
