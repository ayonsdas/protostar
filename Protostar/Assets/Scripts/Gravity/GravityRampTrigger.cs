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
            ApplyGravityChange(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!triggerOnEnter && other.CompareTag("Player"))
        {
            ApplyGravityChange(other);
        }
    }

    private void ApplyGravityChange(Collider other)
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

        CustomGravityBody gravityBody = other.GetComponent<CustomGravityBody>();

        // If the player has a gravity body, set the custom gravity direction for that body only
        if (gravityBody != null)
        {
            gravityBody.SetCustomGravityDirection(newGravityDirection);
        }

        // Otherwise, we set the global gravity direction in the controller
        else
        {
            GravityController.Instance.SetGravityDirection(newGravityDirection);
        }

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
