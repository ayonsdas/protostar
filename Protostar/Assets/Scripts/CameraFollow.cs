using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Assign the player in the Inspector

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 5f, -10f); // Position behind and above player
    public float smoothTime = 0.05f; // How smoothly camera follows

    [Header("Camera Collision")]
    public LayerMask collisionLayers; // Layers to check for obstacles
    public float collisionRadius = 0.3f; // Radius of the camera sphere for collision
    public float collisionSmoothTime = 0.1f; // How smoothly camera adjusts to obstacles
    public float minDistance = 0.5f; // Minimum distance camera can be from player

    private Vector3 velocity = Vector3.zero; // Used by SmoothDamp

    [Header("Camera Rotation Settings")]
    public float rotationSpeed = 50f;
    public float returnDelay = 3f; // Seconds before returning to default
    public float returnSpeed = 2f; // Speed of return to default

    [Header("Vertical Angle Limits")]
    public float minPitch = -30f;  // Minimum elevation angle (below horizontal)
    public float maxPitch = 75f;   // Maximum elevation angle (never directly above)

    private InputAction lookAction;
    private InputAction mouseHoldAction;
    private CustomGravityBody gravityBody;
    private Vector3 cameraDir;        // World-space normalized direction from player to camera
    private float cameraDistance;     // Distance from player (from offset magnitude)
    private Vector3 lastGravityUp;    // Previous gravity up, for detecting changes
    private float timeSinceLastInput = 0f;
    private float timeSinceLastMovement = 0f;
    private bool isReturning = false;
    private float currentDistance = 0f;
    private float distanceVelocity = 0f;
    private Vector3 lastPlayerPosition;
    private Vector2 lookInput = Vector2.zero;
    private bool isMouseHeld = false;

    void Start()
    {
        if (target != null)
        {
            // Enable interpolation on the player's Rigidbody to reduce jitter
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            gravityBody = target.GetComponent<CustomGravityBody>();

            // Initialize camera direction from the local offset converted to world space
            Vector3 worldOffset = target.TransformDirection(offset);
            cameraDir = worldOffset.normalized;
            cameraDistance = worldOffset.magnitude;
            currentDistance = cameraDistance;
            lastGravityUp = gravityBody != null ? gravityBody.GetUpDirection() : Vector3.up;
            lastPlayerPosition = target.position;
        }
    }

    private void OnEnable()
    {
        if (InputModeManager.Instance == null || InputModeManager.Instance.PlayerInput == null)
        {
            Debug.LogWarning($"[PlayerController] Cannot find PlayerInput {InputModeManager.Instance}");
        }
        else
        {
            PlayerInput playerInput = InputModeManager.Instance.PlayerInput;
            playerInput.actions["Look"].performed += OnLook;
            playerInput.actions["Look"].canceled += OnLook;
            playerInput.actions["MouseHold"].performed += OnMouseHold;
            playerInput.actions["MouseHold"].canceled += OnMouseHold;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Camera no longer rotates to match gravity changes
        Vector3 gravityUp = Vector3.up; // Always use world up for camera orientation
        // Do not rotate cameraDir when gravity changes
        // lastGravityUp = gravityUp; // Not needed

        // Check if player has moved
        bool playerMoved = Vector3.Distance(target.position, lastPlayerPosition) > 0.01f;
        if (playerMoved)
        {
            timeSinceLastMovement = 0f;
            lastPlayerPosition = target.position;
        }
        else
        {
            timeSinceLastMovement += Time.deltaTime;
        }

        // Update camera direction when player is actively moving the mouse (no RMB requirement)
        if (lookInput.magnitude > 0.01f)
        {
            // Horizontal: rotate around gravity up axis
            float mouseSensitivity = SettingsManager.Instance.MouseSensitivity;
            Debug.Log($"[CameraFollow] moving camera with sensetivity {mouseSensitivity}");
            Quaternion yawRot = Quaternion.AngleAxis(lookInput.x * mouseSensitivity * rotationSpeed * Time.deltaTime, gravityUp);
            cameraDir = (yawRot * cameraDir).normalized;

            // Vertical: rotate around the right axis (perpendicular to gravity up and camera dir)
            Vector3 right = Vector3.Cross(gravityUp, cameraDir).normalized;
            if (right.sqrMagnitude > 0.001f)
            {
                Quaternion pitchRot = Quaternion.AngleAxis(lookInput.y * mouseSensitivity * rotationSpeed * Time.deltaTime, right);
                Vector3 newDir = (pitchRot * cameraDir).normalized;

                // Only accept if within pitch limits
                float angle = Vector3.Angle(newDir, gravityUp);
                float minAngleFromUp = 90f - maxPitch; // e.g. 15° from straight up
                float maxAngleFromUp = 90f - minPitch; // e.g. 85° from straight up
                if (angle >= minAngleFromUp && angle <= maxAngleFromUp)
                {
                    cameraDir = newDir;
                }
            }

            // Reset both timers when camera is moved
            timeSinceLastInput = 0f;
            timeSinceLastMovement = 0f;
            isReturning = false;
        }
        else
        {
            timeSinceLastInput += Time.deltaTime;

            // Start returning ONLY after 3 seconds of BOTH no input AND no player movement
            if (timeSinceLastInput >= returnDelay && timeSinceLastMovement >= returnDelay)
            {
                isReturning = true;
            }
        }

        // Smoothly return to behind player (cancel if player moves during reset)
        if (isReturning)
        {
            if (playerMoved)
            {
                // Player started moving during reset — cancel and restart timers
                isReturning = false;
                timeSinceLastInput = 0f;
                timeSinceLastMovement = 0f;
            }
            else
            {
                // Target direction: behind player in world space
                Vector3 targetDir = target.TransformDirection(offset).normalized;

                // Smooth interpolation
                cameraDir = Vector3.Slerp(cameraDir, targetDir, returnSpeed * Time.deltaTime);

                // Stop returning when close enough
                if (Vector3.Angle(cameraDir, targetDir) < 1f)
                {
                    cameraDir = targetDir;
                    isReturning = false;
                }
            }
        }

        // --- Position camera ---
        float desiredDistance = cameraDistance;
        float targetDistance = desiredDistance;

        // Check for obstacles between camera and player
        if (collisionLayers.value != 0)
        {
            RaycastHit hit;
            if (Physics.SphereCast(target.position, collisionRadius, cameraDir, out hit, desiredDistance, collisionLayers))
            {
                targetDistance = Mathf.Max(hit.distance - collisionRadius, minDistance);
            }
        }

        // Smoothly adjust distance
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, collisionSmoothTime);

        // Apply adjusted distance
        Vector3 finalPosition = target.position + cameraDir * currentDistance;

        // Smoothly move camera
        transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref velocity, smoothTime);

        // Camera looks at player, using world up for orientation
        Vector3 directionToTarget = target.position - transform.position;
        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
            transform.rotation = lookRotation;
        }
    }

    /// <summary>
    /// Clamp the camera direction so it stays within pitch limits relative to the given up direction.
    /// </summary>
    private void ClampPitch(ref Vector3 dir, Vector3 up)
    {
        float angle = Vector3.Angle(dir, up);
        float minAngleFromUp = 90f - maxPitch;
        float maxAngleFromUp = 90f - minPitch;

        if (angle < minAngleFromUp)
        {
            // Too close to straight up — push away from up
            Vector3 right = Vector3.Cross(up, dir);
            if (right.sqrMagnitude < 0.001f)
            {
                // dir is parallel to up, pick an arbitrary perpendicular
                right = Vector3.Cross(up, target.forward);
                if (right.sqrMagnitude < 0.001f)
                    right = Vector3.Cross(up, Vector3.right);
            }
            right = right.normalized;
            dir = Quaternion.AngleAxis(minAngleFromUp, right) * up;
            dir = dir.normalized;
        }
        else if (angle > maxAngleFromUp)
        {
            // Too close to horizontal/below — push toward up
            Vector3 right = Vector3.Cross(up, dir).normalized;
            dir = Quaternion.AngleAxis(maxAngleFromUp, right) * up;
            dir = dir.normalized;
        }
    }

    private void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    private void OnMouseHold(InputAction.CallbackContext ctx)
    {
        isMouseHeld = ctx.performed;
    }
}
