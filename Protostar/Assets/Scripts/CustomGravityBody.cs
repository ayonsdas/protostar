using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityBody : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3? customGravityDirection = null; // If set, uses this instead of global gravity
    private float gravityStrength = 100f; // Default strength

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Disable Unity's built-in gravity so we can apply our own
        rb.useGravity = false;
        
        // Get gravity strength from controller if available
        if (GravityController.Instance != null)
        {
            gravityStrength = GravityController.Instance.GetGravity().magnitude;
        }
    }

    void FixedUpdate()
    {
        // If we have a custom gravity direction, use that
        if (customGravityDirection.HasValue)
        {
            Vector3 gravity = customGravityDirection.Value.normalized * gravityStrength;
            rb.AddForce(gravity, ForceMode.Acceleration);
        }
        // Otherwise use global gravity
        else if (GravityController.Instance != null)
        {
            // Apply custom gravity force
            Vector3 gravity = GravityController.Instance.GetGravity();
            rb.AddForce(gravity, ForceMode.Acceleration);
        }
    }
    
    /// <summary>
    /// Set a custom gravity direction for this object (independent of global gravity)
    /// </summary>
    public void SetCustomGravityDirection(Vector3 direction)
    {
        customGravityDirection = direction.normalized;
    }
    
    /// <summary>
    /// Clear custom gravity and use global gravity again
    /// </summary>
    public void ClearCustomGravity()
    {
        customGravityDirection = null;
    }
    
    /// <summary>
    /// Check if this object has custom gravity set
    /// </summary>
    public bool HasCustomGravity()
    {
        return customGravityDirection.HasValue;
    }

    public Vector3 GetGravityDirection()
    {
        // If we have custom gravity, return that
        if (customGravityDirection.HasValue)
        {
            return customGravityDirection.Value;
        }
        
        // Otherwise return global gravity direction
        if (GravityController.Instance != null)
        {
            return GravityController.Instance.GetGravity().normalized;
        }
        return Vector3.down;
    }

    public Vector3 GetUpDirection()
    {
        // "Up" is opposite to gravity
        return -GetGravityDirection();
    }
}
