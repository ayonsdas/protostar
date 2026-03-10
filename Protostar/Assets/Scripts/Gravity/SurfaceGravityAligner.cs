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
        RaycastHit downHit, velocityHit;

        bool hasDownHit = Physics.Raycast(
            transform.position,
            gravityDir,
            out downHit,
            downRaycastDistance,
            surfaceLayerMask
        );

        Vector3 moveDir = rb.linearVelocity.magnitude > velocityThreshold
            ? Vector3.ProjectOnPlane(rb.linearVelocity.normalized, gravityDir).normalized
            : Vector3.zero;

        // Cast straight down from a point ahead of the player's feet
        Vector3 lookaheadOrigin = transform.position + moveDir * lookaheadDistance;

        // Not returning early since we need the hit to be assigned
        bool hasLookaheadHit = Physics.Raycast(
            lookaheadOrigin,
            gravityDir,
            out velocityHit,
            velocityRaycastDistance,
            surfaceLayerMask
        )
        && rb.linearVelocity.magnitude > velocityThreshold;

        // Pure velocity cast — only used airborne
        RaycastHit airborneHit;
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
        Debug.DrawRay(transform.position, gravityDir * downRaycastDistance, hasDownHit ? Color.green : Color.red);
        Debug.DrawRay(lookaheadOrigin, gravityDir * velocityRaycastDistance,
            hasLookaheadHit ? Color.yellow : Color.white);
        Debug.DrawRay(transform.position, rb.linearVelocity.normalized * velocityRaycastDistance,
            hasAirborneHit ? Color.yellow : Color.white);

        // Pick the closer hit
        RaycastHit? best = null;
        if (hasDownHit && hasLookaheadHit)
        {
            // How much does each hit want to change our gravity?
            float downDelta = Vector3.Angle(gravityDir, -downHit.normal);
            float velDelta = Vector3.Angle(gravityDir, -velocityHit.normal);
            //Debug.Log($"[SurfaceGravityAligner] Down Delta: {downDelta}, Velocity Delta: {velDelta}");

            // Prefer the hit that represents the biggest correction needed
            best = velDelta > downDelta ? velocityHit : downHit;
        }
        else if (hasDownHit)
        {
            //Debug.Log($"[SurfaceGravityAligner] Down hit found with normal: {downHit.normal}");
            best = downHit;
        }
        else if (hasAirborneHit)
        {
            //Debug.Log($"[SurfaceGravityAligner] Airborne hit found with normal: {airborneHit.normal}");
            best = airborneHit;
        }
        else if (hasLookaheadHit)
        {
            //Debug.Log($"[SurfaceGravityAligner] Lookahead hit found with normal: {velocityHit.normal}");
            best = velocityHit;
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
            rb.AddForce(-best.Value.normal * groundStickinessForce, ForceMode.Acceleration);
        }
    }
}