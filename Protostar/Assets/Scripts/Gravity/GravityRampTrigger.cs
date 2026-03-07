using UnityEngine;

public class GravityRampTrigger : MonoBehaviour
{
    [Header("Ramp Settings")]
    [Tooltip("The direction gravity should point when on this ramp. Leave as (0,0,0) to auto-calculate from ramp orientation.")]
    public Vector3 gravityDirection = Vector3.zero;
    
    [Tooltip("If true, gravity will point down along the ramp's local -Y axis")]
    public bool useRampOrientation = true;
    
    [Header("Trigger Settings")]
    [Tooltip("If true, only triggers when entering. If false, only triggers when exiting.")]
    public bool triggerOnEnter = true;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnEnter && other.CompareTag("Player"))
        {
            ApplyGravityChange();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!triggerOnEnter && other.CompareTag("Player"))
        {
            ApplyGravityChange();
        }
    }
    
    private void ApplyGravityChange()
    {
        Vector3 newGravityDirection;
        
        if (useRampOrientation)
        {
            // Use the ramp's down direction (negative local Y axis)
            newGravityDirection = -transform.up;
        }
        else
        {
            // Use the manually specified direction
            newGravityDirection = gravityDirection.normalized;
        }
        
        // Set the new gravity direction
        GravityController.Instance.SetGravityDirection(newGravityDirection);
        
        Debug.Log($"Gravity changed to: {newGravityDirection}");
    }

    private void OnDrawGizmos()
    {
        // Visualize the gravity direction this ramp will apply
        Vector3 direction = useRampOrientation ? -transform.up : gravityDirection.normalized;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 3f);
    }
}
