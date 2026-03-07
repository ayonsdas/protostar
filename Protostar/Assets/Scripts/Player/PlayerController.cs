using System;
using FMOD.Studio;
using FMODUnity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CustomGravityBody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSpeed = 100f;
    public float acceleration = 40f;
    public float airAccelerationMultiplier = 0.4f;

    [Header("Jump Settings")]
    public float jumpForce = 5f;
    [SerializeField] private float jumpCooldownTime = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    public float groundCheckRadius = 0.3f;
    public Transform groundCheck; // Create an empty child object at player's feet
    [Tooltip("Layers to check for ground. Set to 'Everything' to jump off any object, or specific layers to limit what counts as ground.")]
    public LayerMask groundLayer = ~0; // Default to all layers (~0 = everything)

    [Header("Gravity Rotation Settings")]
    public float gravityRotationSpeed = 2f; // How fast player rotates to match gravity
    [Header("Sound Settings")]
    [SerializeField] private bool disableFootsteps = false;
    [SerializeField] private float footstepSpeedThreshold = 0.01f;
    [SerializeField] private float footstepDebounceTime = 0.5f;
    [SerializeField] private EventReference footstepEventReference;
    [SerializeField] private EventReference jumpEventReference;
    [SerializeField] private float landCooldownTime = 0.5f;
    [SerializeField] private EventReference landEventReference;

    public event Action<bool> OnGroundedChanged;

    private bool _isGrounded;
    public bool IsGrounded
    {
        get => _isGrounded;
        private set
        {
            if (_isGrounded == value) return;

            _isGrounded = value;
            OnGroundedChanged?.Invoke(value);
        }
    }
    private bool CanJump => coyoteTimer.IsActive && !jumpCooldownTimer.IsActive;
    private bool CanGroundedJump => IsGrounded && !jumpCooldownTimer.IsActive;
    private Vector2 moveInput;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    public event Action OnJumpSuccess;
    public float GetNormalizedSpeed => Mathf.Clamp(rb.linearVelocity.magnitude / moveSpeed, 0f, 1f);
    private Rigidbody rb;
    private CustomGravityBody gravityBody;
    private EventInstance footstepEventInstance;
    private float footstepStartTime;
    private Transform cameraTransform;
    private bool movementLocked = false;
    private Vector3 movementBaseGravity = Vector3.down; // Gravity direction at start of movement
    private bool wasMoving = false; // Track if player was moving last frame
    private Timer landSFXCooldownTimer;
    private Timer jumpCooldownTimer;
    private Timer coyoteTimer;
    private InputBuffer jumpBuffer;

    [Header("Ground/Gravity Check Collider")]
    [Tooltip("Assign the BoxCollider used for ground and gravity checks. Only this collider will be used for detection.")]
    public BoxCollider groundCheckBoxCollider;

    private bool CanLand => !landSFXCooldownTimer.IsActive;

    /// <summary>
    /// Lock or unlock player movement. When locked, WASD input is ignored for movement.
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (locked)
        {
            moveInput = Vector2.zero;
            // Kill horizontal velocity immediately
            if (rb != null && gravityBody != null)
            {
                Vector3 gravityDir = gravityBody.GetGravityDirection();
                float verticalComponent = Vector3.Dot(rb.linearVelocity, gravityDir);
                rb.linearVelocity = gravityDir * verticalComponent;
            }
        }
    }

    public bool IsMovementLocked() => movementLocked;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gravityBody = GetComponent<CustomGravityBody>();

        // Enable continuous collision detection for better trigger detection
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // If no groundCheckBoxCollider is assigned, try to get one from this GameObject
        if (groundCheckBoxCollider == null)
        {
            groundCheckBoxCollider = GetComponent<BoxCollider>();
        }
        // If no groundCheck transform is assigned, create one at the bottom of the specified BoxCollider
        if (groundCheck == null && groundCheckBoxCollider != null)
        {
            GameObject checkObj = new GameObject("GroundCheck");
            checkObj.transform.parent = transform;
            float colliderBottom = -(groundCheckBoxCollider.size.y / 2f) + 0.1f;
            checkObj.transform.localPosition = new Vector3(0, colliderBottom, 0);
            groundCheck = checkObj.transform;
        }

        // Initialize timers
        jumpCooldownTimer = new Timer(jumpCooldownTime);
        landSFXCooldownTimer = new Timer(landCooldownTime);
        coyoteTimer = new Timer(coyoteTime);
        jumpBuffer = new InputBuffer(jumpBufferTime);
    }

    void Start()
    {
        footstepEventInstance = AudioManager.Instance.CreateEventInstance(footstepEventReference);
        footstepStartTime = Time.time;

        // Get reference to main camera
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        bool previouslyGrounded = IsGrounded;
        Vector3 gravityDown = gravityBody.GetGravityDirection();

        // Gravity check: use BoxCollider
        bool gravityGrounded = false;
        if (groundCheckBoxCollider != null)
        {
            Vector3 boxBottomCenter = groundCheckBoxCollider.bounds.center + gravityDown * (-groundCheckBoxCollider.bounds.extents.y + 0.01f);
            RaycastHit hit;
            if (Physics.BoxCast(boxBottomCenter, groundCheckBoxCollider.bounds.extents * 0.95f, gravityDown, out hit, groundCheckBoxCollider.transform.rotation, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != groundCheckBoxCollider)
                {
                    gravityGrounded = true;
                }
            }
        }

        // Jump check: use SphereCollider at groundCheck.position
        bool foundGround = false;
        if (groundCheck != null)
        {
            Collider[] colliders = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
            foreach (Collider col in colliders)
            {
                if (col != groundCheckBoxCollider)
                {
                    if (!previouslyGrounded && CanLand)
                    {
                        OnLand();
                    }
                    foundGround = true;
                    break;
                }
            }
        }
        IsGrounded = foundGround;

        if (CanGroundedJump)
        {
            // Use buffered jump if we have it
            if (jumpBuffer.Consume())
            {
                Debug.Log($"[PlayerController] Using buffered jump, starting with velocity {rb.linearVelocity}");
                HandleJumpSuccess();
            }
            // Only restart coyote time if not using buffered jump
            else
            {
                coyoteTimer.Restart();
            }
        }

        // Debug visualization
        Color debugColor = IsGrounded ? Color.green : Color.red;
        Debug.DrawLine(groundCheck.position, groundCheck.position + gravityDown * groundCheckRadius, debugColor);

        UpdateSound();
    }

    private void OnEnable()
    {
        if (!InputModeManager.HasPlayerInput)
        {
            Debug.LogWarning($"[PlayerController] Cannot find PlayerInput");
        }
        else
        {
            PlayerInput playerInput = InputModeManager.PlayerInput;
            playerInput.actions["Move"].performed += OnMove;
            playerInput.actions["Move"].canceled += OnMove;
            playerInput.actions["Jump"].performed += OnJump;
            playerInput.actions["RotateGravity"].performed += OnRotateGravity;
        }
    }

    private void OnDisable()
    {
        if (!InputModeManager.HasPlayerInput)
        {
            Debug.LogWarning($"[PlayerController] Cannot find PlayerInput");
        }
        else
        {
            PlayerInput playerInput = InputModeManager.PlayerInput;
            playerInput.actions["Move"].performed -= OnMove;
            playerInput.actions["Move"].canceled -= OnMove;
            playerInput.actions["Jump"].performed -= OnJump;
            playerInput.actions["RotateGravity"].performed -= OnRotateGravity;
        }
    }

    void AlignToGravity()
    {
        // Get the "up" direction (opposite of gravity)
        Vector3 up = gravityBody.GetUpDirection();

        // Get current up direction
        Vector3 currentUp = transform.up;

        // Only align if there's a significant difference
        if (Vector3.Angle(currentUp, up) > 0.1f)
        {
            // Calculate target rotation to align player's up with gravity's up
            // Keep the player's forward direction as much as possible
            Vector3 forward = transform.forward;
            Vector3 projectedForward = Vector3.ProjectOnPlane(forward, up);

            Quaternion targetRotation = transform.rotation;
            if (projectedForward.sqrMagnitude > 0.01f)
            {
                targetRotation = Quaternion.LookRotation(projectedForward, up);
            }
            else
            {
                // If forward is parallel to up, use right vector instead
                Vector3 right = transform.right;
                Vector3 projectedRight = Vector3.ProjectOnPlane(right, up);
                if (projectedRight.sqrMagnitude > 0.01f)
                {
                    targetRotation = Quaternion.LookRotation(Vector3.Cross(up, projectedRight), up);
                }
            }

            // Smoothly interpolate to target rotation using Rigidbody
            Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, gravityRotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }
    }

    void FixedUpdate()
    {
        // Get camera reference if we don't have it
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Get gravity info used throughout method
        Vector3 upDirection = gravityBody.GetUpDirection();
        Vector3 gravityDirection = -upDirection;

        // Track if movement just started - if so, store current gravity as base
        bool isMoving = moveInput.magnitude > 0.01f;
        if (isMoving && !wasMoving)
        {
            // Movement just started - store current gravity as the base
            movementBaseGravity = gravityDirection;
        }
        else if (!isMoving && wasMoving)
        {
            // Movement just stopped - reset base to current gravity for next time
            movementBaseGravity = gravityDirection;
        }
        wasMoving = isMoving;

        // Calculate movement direction relative to camera or gravity
        Vector3 moveDirection = Vector3.zero;
        if (cameraTransform != null && moveInput.magnitude > 0.01f)
        {
            // Apply gravity delta to maintain direction when traversing curves
            // Use base gravity from start of movement instead of Vector3.down
            Quaternion gravityDelta = Quaternion.FromToRotation(movementBaseGravity, gravityDirection);

            // Check if player is on a wall (gravity roughly horizontal)
            float verticalAlignment = Mathf.Abs(Vector3.Dot(gravityDirection, Vector3.up));
            bool isOnWall = verticalAlignment < 0.3f; // Gravity is mostly horizontal

            // Check if we're in "top-down mode" - camera looking along gravity axis
            Vector3 cameraForwardWorld = cameraTransform.forward;
            float cameraGravityAlignment = Mathf.Abs(Vector3.Dot(cameraForwardWorld, gravityDirection));
            bool isTopDownMode = cameraGravityAlignment > 0.7f; // Camera looking mostly up/down gravity axis

            Vector3 forward, right;

            if (isOnWall)
            {
                // WALL MODE: Special handling when on a wall (90-degree gravity)
                // W = away from camera, S = toward camera
                // A = left, D = right

                // Forward: direction from camera to player
                Vector3 cameraToPlayer = (transform.position - cameraTransform.position).normalized;
                Vector3 cameraToPlayerTransformed = gravityDelta * cameraToPlayer;
                Vector3 forwardProjected = Vector3.ProjectOnPlane(cameraToPlayerTransformed, upDirection);

                if (forwardProjected.sqrMagnitude > 0.01f)
                {
                    forward = forwardProjected.normalized;
                }
                else
                {
                    // Camera directly aligned with gravity - use camera forward
                    Vector3 cameraForwardTransformed = gravityDelta * cameraTransform.forward;
                    forwardProjected = Vector3.ProjectOnPlane(cameraForwardTransformed, upDirection);
                    forward = forwardProjected.sqrMagnitude > 0.01f ? forwardProjected.normalized : Vector3.ProjectOnPlane(transform.forward, upDirection).normalized;
                }

                // Right: camera's right vector
                Vector3 cameraRightTransformed = gravityDelta * cameraTransform.right;
                Vector3 rightProjected = Vector3.ProjectOnPlane(cameraRightTransformed, upDirection);

                if (rightProjected.sqrMagnitude > 0.01f)
                {
                    right = rightProjected.normalized;
                }
                else
                {
                    // Camera right aligned with gravity - derive from forward
                    right = Vector3.Cross(upDirection, forward).normalized;
                }

                // Ensure orthogonality: re-orthogonalize right to be perpendicular to forward
                // Project right onto the plane perpendicular to forward
                Vector3 rightOrthogonal = Vector3.ProjectOnPlane(right, forward);
                if (rightOrthogonal.sqrMagnitude > 0.01f)
                {
                    right = rightOrthogonal.normalized;
                }
                else
                {
                    // If right was parallel to forward, compute it from cross product
                    right = Vector3.Cross(upDirection, forward).normalized;
                }

                // Check if camera is above/below player relative to wall orientation
                // Use the non-transformed camera position for this check
                Vector3 cameraToPlayerWorld = transform.position - cameraTransform.position;

                // Check if forward direction makes sense for "away from camera"
                // It should have a positive dot product with cameraToPlayer
                float forwardAlignment = Vector3.Dot(forward, cameraToPlayerTransformed);
                if (forwardAlignment < -0.1f)
                {
                    // Forward points toward camera instead of away - flip it
                    forward = -forward;
                    // Also flip right to maintain consistent handedness
                    right = -right;
                }
            }
            else if (isTopDownMode)
            {
                // TOP-DOWN MODE: Camera is looking along gravity axis (like overhead view on a wall)
                // W/S should move up/down the screen (camera forward/back)
                // A/D should move left/right the screen (camera left/right)

                Vector3 cameraForwardTransformed = gravityDelta * cameraTransform.forward;
                Vector3 cameraRightTransformed = gravityDelta * cameraTransform.right;

                Vector3 forwardProjected = Vector3.ProjectOnPlane(cameraForwardTransformed, upDirection);
                Vector3 rightProjected = Vector3.ProjectOnPlane(cameraRightTransformed, upDirection);

                forward = forwardProjected.sqrMagnitude > 0.01f ? forwardProjected.normalized : Vector3.ProjectOnPlane(transform.forward, upDirection).normalized;
                right = rightProjected.sqrMagnitude > 0.01f ? rightProjected.normalized : Vector3.Cross(upDirection, forward).normalized;
            }
            else
            {
                // NORMAL MODE: Camera looking at player from the side
                // W/S should move away/toward camera
                // A/D should move left/right relative to camera

                // Forward: direction from camera to player (transformed for curves)
                Vector3 cameraToPlayer = (transform.position - cameraTransform.position).normalized;
                Vector3 cameraToPlayerTransformed = gravityDelta * cameraToPlayer;
                Vector3 forwardProjected = Vector3.ProjectOnPlane(cameraToPlayerTransformed, upDirection);

                if (forwardProjected.sqrMagnitude > 0.01f)
                {
                    forward = forwardProjected.normalized;
                }
                else
                {
                    // Camera directly above/below - use camera forward
                    Vector3 cameraForwardTransformed = gravityDelta * cameraTransform.forward;
                    forwardProjected = Vector3.ProjectOnPlane(cameraForwardTransformed, upDirection);
                    forward = forwardProjected.sqrMagnitude > 0.01f ? forwardProjected.normalized : Vector3.ProjectOnPlane(transform.forward, upDirection).normalized;
                }

                // Right: camera's right vector (transformed for curves)
                Vector3 cameraRightTransformed = gravityDelta * cameraTransform.right;
                Vector3 rightProjected = Vector3.ProjectOnPlane(cameraRightTransformed, upDirection);

                right = rightProjected.sqrMagnitude > 0.01f ? rightProjected.normalized : Vector3.Cross(upDirection, forward).normalized;
            }

            // Calculate movement direction from input
            // W (moveInput.y > 0) = forward
            // S (moveInput.y < 0) = backward
            // D (moveInput.x > 0) = right
            // A (moveInput.x < 0) = left
            Vector3 camMove = forward * moveInput.y + right * moveInput.x;
            if (camMove.sqrMagnitude > 0.01f) camMove.Normalize();
            moveDirection = camMove;
            // Rotate player to face movement direction
            if (moveDirection.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, upDirection);
                Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime * 0.1f);
                rb.MoveRotation(newRotation);
            }
        }

        // Use linearVelocity for proper collision detection
        Vector3 currentVelocity = rb.linearVelocity;

        // Keep the vertical (gravity-aligned) component of velocity
        float verticalComponent = Vector3.Dot(currentVelocity, gravityDirection);
        Vector3 horizontalVelocity = currentVelocity - (gravityDirection * verticalComponent);

        // Desired horizontal velocity
        Vector3 desiredVelocity = moveDirection * moveSpeed;

        // Velocity difference
        Vector3 velocityChange = desiredVelocity - horizontalVelocity;

        // Use acceleration multiplier to lower air acceleration
        float accelerationMultiplier = IsGrounded ? 1 : airAccelerationMultiplier;

        // Apply force towards new velocity (Only in horizontal plane)
        rb.AddForce(velocityChange * acceleration * accelerationMultiplier, ForceMode.Acceleration);

        // Smoothly align player to gravity direction (after turning so it doesn't override)
        AlignToGravity();
    }

    // Called by Player Input component (Send Messages behavior)
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!movementLocked)
        {
            moveInput = ctx.ReadValue<Vector2>();
        }
    }

    // Called by Player Input component (Send Messages behavior)
    public void OnJump(InputAction.CallbackContext ctx)
    {
        // If jump button released, ignore
        if (!ctx.performed)
            return;

        // If jump button pressed, but not able to use, buffer it
        if (!CanJump)
        {
            jumpBuffer.Press();
            return;
        }

        // Otherwise, jump like normal
        HandleJumpSuccess();
    }

    private void HandleJumpSuccess()
    {
        jumpCooldownTimer.Restart();
        coyoteTimer.Stop();
        jumpBuffer.Consume();

        ApplyJumpForce();

        AudioManager.Instance.PlayOneShot(jumpEventReference, gameObject.transform.position);

        OnJumpSuccess?.Invoke();
    }

    // Called by playerAnimationController to sync with jump animation
    public void ApplyJumpForce()
    {
        Vector3 jumpDirection = gravityBody.GetUpDirection();

        // Strip downward velocity so jump goes upward right when landing
        Vector3 currentVelocity = rb.linearVelocity;
        float verticalComponent = Vector3.Dot(currentVelocity, jumpDirection);
        if (verticalComponent < 0)
        {
            rb.linearVelocity = currentVelocity - (jumpDirection * verticalComponent);
        }

        rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
        Debug.Log($"[PlayerController] Jumping, velocity after force: {rb.linearVelocity}");
    }

    // Called by Player Input component (Send Messages behavior)
    public void OnRotateGravity(InputAction.CallbackContext ctx)
    {
        Debug.Log($"OnRotateGravity called! isPressed: {ctx.performed}");

        if (ctx.performed && GravityController.Instance != null)
        {
            Debug.Log("Rotating gravity!");
            // Rotate gravity 90 degrees around the X axis
            GravityController.Instance.RotateGravityAroundAxis(Vector3.right, 90f);
        }
        else
        {
            if (GravityController.Instance == null)
                Debug.LogError("GravityController.Instance is null!");
        }
    }

    private bool CanStartFootsteps()
    {
        bool moving = rb.linearVelocity.magnitude >= footstepSpeedThreshold;
        bool debounce = Time.time - footstepStartTime > footstepDebounceTime;
        bool justLanded = landSFXCooldownTimer.IsActive;
        return moving && debounce && IsGrounded && !justLanded;
    }

    private void UpdateSound()
    {
        if (disableFootsteps) return;
        if (CanStartFootsteps())
        {
            PLAYBACK_STATE playbackState;
            footstepEventInstance.getPlaybackState(out playbackState);
            if (playbackState == PLAYBACK_STATE.STOPPED)
            {
                //Debug.Log("Started footsteps Velocity: " + rb.linearVelocity + " Grounded: " + isGrounded);
                footstepStartTime = Time.time;
                footstepEventInstance.start();
            }
        }
    }

    private void OnLand()
    {
        landSFXCooldownTimer.Restart();
        PlayLandSound();
    }
    private void PlayLandSound()
    {
        if (AudioManager.Instance != null && !landEventReference.IsNull)
            AudioManager.Instance.PlayOneShot(landEventReference, gameObject.transform.position);

        else
            Debug.LogWarning("[PlayerController] Landing sound not assigned");
    }
}
