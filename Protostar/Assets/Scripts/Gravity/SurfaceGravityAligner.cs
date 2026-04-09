using UnityEngine;

[RequireComponent(typeof(CustomGravityBody))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerController))]
public class SurfaceGravityAligner : MonoBehaviour
{
    [Header("Flip Settings")]
    [SerializeField] private float downRaycastDistance = 2f;
    [SerializeField] private float velocityRaycastDistance = 2f;
    [SerializeField] private float velocityThreshold = 0.5f;
    [SerializeField] private float lookaheadDistance = 0.5f;
    [SerializeField] private LayerMask surfaceLayerMask = ~0;

    [Header("Smoothing Settings")]
    [SerializeField] private float alignmentSpeed = 8f;
    [Header("Stickiness Settings")]
    [SerializeField] private float groundStickinessForce = 20f;

    private CustomGravityBody gravityBody;
    private Rigidbody rb;
    private PlayerController playerController;

    void Awake()
    {
        gravityBody = GetComponent<CustomGravityBody>();
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
    }

    void FixedUpdate()
    {

        // Raycast downward in the player's current gravity direction
        Vector3 gravityDir = gravityBody.GetGravityDirection();
        RaycastHit lookaheadHit, airborneHit;

        Vector3 moveDir = rb.linearVelocity.magnitude > velocityThreshold
            ? Vector3.ProjectOnPlane(rb.linearVelocity.normalized, gravityDir).normalized
            : Vector3.zero;

        // Cast straight down from a point ahead of the player's feet
        Vector3 lookaheadOrigin = transform.position + moveDir * lookaheadDistance;

        // Not returning early since we need the hit to be assigned
        bool hasLookaheadHit = Physics.Raycast(
            lookaheadOrigin,
            gravityDir,
            out lookaheadHit,
            velocityRaycastDistance,
            surfaceLayerMask
        )
        && rb.linearVelocity.magnitude > velocityThreshold;

        // Pure velocity cast — only used airborne
        bool hasAirborneHit = Physics.Raycast(
                transform.position,
                rb.linearVelocity.normalized,
                out airborneHit,
                velocityRaycastDistance,
                surfaceLayerMask
            ) &&
            rb.linearVelocity.magnitude > velocityThreshold &&
            !playerController.IsGrounded;

        // Draw debug rays to visualize the casts
        Debug.DrawRay(lookaheadOrigin, gravityDir * velocityRaycastDistance,
            hasLookaheadHit ? Color.yellow : Color.white);
        Debug.DrawRay(transform.position, rb.linearVelocity.normalized * velocityRaycastDistance,
            hasAirborneHit ? Color.yellow : Color.white);

        // Pick the closer hit
        RaycastHit? best = null;
        if (hasLookaheadHit)
        {
            // How much does each hit want to change our gravity?
            // float velDelta = Vector3.Angle(gravityDir, -lookaheadHit.normal);
            //Debug.Log($"[SurfaceGravityAligner] Down Delta: {downDelta}, Velocity Delta: {velDelta}");

            // Prefer the hit that represents the biggest correction needed
            best = lookaheadHit;
        }
        else if (hasAirborneHit)
        {
            //Debug.Log($"[SurfaceGravityAligner] Airborne hit found with normal: {airborneHit.normal}");
            best = airborneHit;
        }

        // Smoothly slerp toward the surface normal
        if (best.HasValue && best.Value.collider.CompareTag("GravityFlip"))
        {
            Vector3 currentGravity = gravityBody.GetGravityDirection();
            Vector3 targetGravity = -best.Value.normal;
            Vector3 newGravity = Vector3.Slerp(currentGravity, targetGravity, Time.fixedDeltaTime * alignmentSpeed);
            gravityBody.SetCustomGravityDirection(newGravity);
            //Debug.Log("[SurfaceGravityAligner] Found surface, aligning gravity to: " + newGravity);
        }

        // If grounded, apply extra force to stick to the surface and prevent sliding on slopes
        if (playerController.IsGrounded && best.HasValue)
        {
            Vector3 surfaceNormal = best.Value.normal;
            float outwardVelocity = Vector3.Dot(rb.linearVelocity, surfaceNormal);

            if (outwardVelocity > 0)
            {
                rb.AddForce(-surfaceNormal * groundStickinessForce, ForceMode.Acceleration);
            }
        }
    }
}