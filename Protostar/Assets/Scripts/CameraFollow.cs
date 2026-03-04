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
    public float returnDelay = 0.5f; // Seconds before returning to default
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
    private bool isCameraFlipped = false;
    private Vector3 currentCameraUp = Vector3.up;
    private Vector3 targetCameraUp = Vector3.up;
    private Vector3 cameraUpVelocity = Vector3.zero;
    private Vector3 cameraBaseGravityUp = Vector3.up; // Camera's base gravity orientation
    
    // Camera input stored as offsets from base orientation
    private float cameraYaw = 0f;   // Horizontal rotation offset
    private float cameraPitch = 0f; // Vertical rotation offset
    private Vector3 baseDirection = Vector3.back; // Base direction in world space (updates only during reset)

    /// <summary>
    /// Returns true if the camera is currently flipped 180 degrees (when player is on ceiling)
    /// </summary>
    public bool IsCameraFlipped() => isCameraFlipped;

    /// <summary>
    /// Returns true if the camera has finished rotating (up vector is close to target)
    /// </summary>
    public bool IsCameraRotationComplete() => Vector3.Angle(currentCameraUp, targetCameraUp) < 5f;

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
            currentCameraUp = Vector3.up;
            targetCameraUp = Vector3.up;
            cameraBaseGravityUp = Vector3.up;
            cameraYaw = 0f;
            cameraPitch = 0f;
            // Initialize base direction to behind player
            baseDirection = -target.forward;
            baseDirection = Vector3.ProjectOnPlane(baseDirection, Vector3.up).normalized;
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

        // Update camera direction when player is actively moving the mouse
        if (lookInput.magnitude > 0.01f)
        {
            float mouseSensitivity = SettingsManager.Instance.MouseSensitivity;
            
            // Accumulate yaw and pitch offsets - these are independent of camera gravity
            cameraYaw += lookInput.x * mouseSensitivity * rotationSpeed * Time.deltaTime;
            cameraPitch += lookInput.y * mouseSensitivity * rotationSpeed * Time.deltaTime; // Invert direction: up is up, down is down
            
            // Clamp pitch to limits
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

            // Reset timers when camera is moved
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
            // Check if player gravity direction is anti-parallel to camera's base gravity
            Vector3 playerUp = gravityBody != null ? gravityBody.GetUpDirection() : Vector3.up;
            Vector3 playerGravity = gravityBody != null ? gravityBody.GetGravityDirection() : Vector3.down;
            Vector3 cameraGravity = -cameraBaseGravityUp;
            
            float angleGravity = Vector3.Angle(playerGravity, cameraGravity);
            bool isAntiParallel = angleGravity > 170f && angleGravity < 190f;
            
            // Flip camera during return if gravity is opposite
            if (isAntiParallel)
            {
                if (!isCameraFlipped)
                {
                    Debug.Log($"[CameraFollow] FLIPPING during return! Player gravity {playerGravity} is anti-parallel to camera gravity {cameraGravity}");
                    isCameraFlipped = true;
                    cameraBaseGravityUp = playerUp;
                    targetCameraUp = playerUp; // Target the new up direction (will smoothly transition)
                }
                else
                {
                    Debug.Log($"[CameraFollow] UN-FLIPPING during return! Player gravity {playerGravity} is anti-parallel to camera gravity {cameraGravity}");
                    isCameraFlipped = false;
                    cameraBaseGravityUp = playerUp;
                    targetCameraUp = playerUp; // Target the new up direction (will smoothly transition)
                }
            }
            
            if (playerMoved)
            {
                // Player started moving during reset — cancel and restart timers
                isReturning = false;
                timeSinceLastInput = 0f;
                timeSinceLastMovement = 0f;
            }
            else
            {
                // Calculate target direction: behind player, projected onto camera gravity plane
                Vector3 playerForward = target.forward;
                Vector3 targetDir = -playerForward;
                targetDir = Vector3.ProjectOnPlane(targetDir, targetCameraUp).normalized;
                
                // Smoothly interpolate base direction to target
                baseDirection = Vector3.Slerp(baseDirection, targetDir, returnSpeed * Time.deltaTime);
                
                // Smoothly reset yaw and pitch to zero
                cameraYaw = Mathf.Lerp(cameraYaw, 0f, returnSpeed * Time.deltaTime);
                cameraPitch = Mathf.Lerp(cameraPitch, 0f, returnSpeed * Time.deltaTime);
                
                // Smoothly rotate camera up to target up (same speed as yaw/pitch reset)
                currentCameraUp = Vector3.Slerp(currentCameraUp, targetCameraUp, returnSpeed * Time.deltaTime);
                currentCameraUp.Normalize();

                // Stop returning when close enough (including camera up)
                if (Vector3.Angle(baseDirection, targetDir) < 1f
                    &&
                    Mathf.Abs(cameraYaw) < 1f
                    &&
                    Mathf.Abs(cameraPitch) < 1f
                    &&
                    Vector3.Angle(currentCameraUp, targetCameraUp) < 1f
                    )
                {
                    baseDirection = targetDir;
                    cameraYaw = 0f;
                    cameraPitch = 0f;
                    currentCameraUp = targetCameraUp;
                    isReturning = false;
                }
            }
        }
        
        // Don't smooth camera up outside of returning - it's handled in the return block
        // currentCameraUp is only modified during isReturning
        
        Debug.Log($"[CameraFollow] CurrentCameraUp: {currentCameraUp}, TargetCameraUp: {targetCameraUp}, Flipped: {isCameraFlipped}");
        
        // Use stored base direction (only updates during reset)
        Vector3 dirFromBase = baseDirection;
        
        // Apply yaw rotation around camera up axis
        Quaternion yawRotation = Quaternion.AngleAxis(cameraYaw, currentCameraUp);
        Vector3 rotatedDir = yawRotation * dirFromBase;
        
        // Apply pitch rotation around right axis (perpendicular to up and rotated dir)
        Vector3 right = Vector3.Cross(currentCameraUp, rotatedDir).normalized;
        Quaternion pitchRotation = Quaternion.AngleAxis(cameraPitch, right);
        cameraDir = (pitchRotation * rotatedDir).normalized;

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

        // Camera looks at player using the calculated up vector
        Vector3 directionToTarget = target.position - transform.position;
        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            // Create rotation using current up vector - this naturally aligns with camera gravity
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget, currentCameraUp);
            transform.rotation = lookRotation;
            
            Debug.Log($"[CameraFollow] Camera Up: {currentCameraUp}, Camera Rotation: {transform.rotation.eulerAngles}, LookRot: {lookRotation.eulerAngles}");
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
