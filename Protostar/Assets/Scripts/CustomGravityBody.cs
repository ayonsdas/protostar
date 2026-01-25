using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityBody : MonoBehaviour
{
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Disable Unity's built-in gravity so we can apply our own
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        if (GravityController.Instance != null)
        {
            // Apply custom gravity force
            Vector3 gravity = GravityController.Instance.GetGravity();
            rb.AddForce(gravity, ForceMode.Acceleration);
        }
    }

    public Vector3 GetGravityDirection()
    {
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
