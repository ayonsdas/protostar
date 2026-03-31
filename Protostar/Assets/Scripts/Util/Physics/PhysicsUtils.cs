using UnityEngine;

public class PhysicsUtils
{
    public static void SeperateVelocity(Rigidbody rb, Vector3 normal, out Vector3 HorizontalVelocity, out Vector3 VerticalVelocity)
    {
        Vector3 velocity = rb.linearVelocity;
        float velocityIntoNormal = Vector3.Dot(velocity, normal);

        VerticalVelocity = velocityIntoNormal * normal;
        HorizontalVelocity = velocity - VerticalVelocity;
    }

    public static float VelocityIntoNormal(Rigidbody rb, Vector3 normal)
    {
        Vector3 velocity = rb.linearVelocity;
        return Vector3.Dot(velocity, normal);
    }
}
