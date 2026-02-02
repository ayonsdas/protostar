using UnityEngine;

/// <summary>
/// Seed object that can be picked up and has custom gravity
/// When dropped, maintains the player's gravity direction at time of drop
/// Initial gravity is the seed's local y-down direction
/// </summary>
[RequireComponent(typeof(CustomGravityBody))]
public class SeedObject : MonoBehaviour, IPickupable
{
    [Header("Colliders")]
    [SerializeField] private Collider pickupTrigger; // Optional: separate trigger collider for easier pickup
    
    private CustomGravityBody gravityBody;
    private Rigidbody rb;
    private Collider[] physicsColliders;
    private bool isPickedUp = false;
    private GameObject currentPicker = null;
    
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
}
