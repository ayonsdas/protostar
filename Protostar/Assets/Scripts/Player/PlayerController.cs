using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
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

    // Store player's last grounded state for respawning on last platform
    public PlayerBodyState LastGroundedState { get; private set; } = new PlayerBodyState();
    private bool CanPlayLandSound => !landSFXCooldownTimer.IsActive;

    private PlatformSurface currentPlatformSurface;

    /// <summary>
    /// Lock or unlock player movement. When locked, WASD input is ignored for movement.
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        Debug.Log("[PlayerController] Set movement lock to " + locked);
        if (locked)
        {
            moveInput = Vector2.zero;
            // Kill horizontal velocity immediately
            if (rb != null && gravityBody != null && !rb.isKinematic)
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
        footstepStartTime = Time.time;

        // Get reference to main camera
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (rb.isKinematic)
            return;

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
        GameObject groundPlatform = null;

        if (groundCheck != null)
        {
            Collider[] colliders = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
            foreach (Collider col in colliders)
            {
                if (col != groundCheckBoxCollider)
                {
                    groundPlatform = col.gameObject;
                    foundGround = true;
                }
            }
        }
        IsGrounded = foundGround;

        if (IsGrounded)
        {
            LastGroundedState = new PlayerBodyState
            {
                Position = transform.position,
                Rotation = transform.rotation,
                GravityDirection = gravityBody.GetGravityDirection(),
                LinearVelocity = rb.linearVelocity,
                AngularVelocity = rb.angularVelocity
            };
        }

        if (IsGrounded != previouslyGrounded)
        {
            // Went from grounded to airborne, reset footstep sound since we aren't on the same platform
            if (!IsGrounded)
            {
                currentPlatformSurface = null;
            }

            else if (groundPlatform != null)
            {
                PlatformSurface surface = groundPlatform.GetComponentInParent<PlatformSurface>();
                currentPlatformSurface = surface;
            }

            UpdatePlatformAudioParameters();
            TryPlayLandSound();
        }



        // Snap to ground if just landed, and falling down
        float velocityAlongGravity = PhysicsUtils.VelocityIntoNormal(rb, gravityDown);
        PhysicsUtils.SeperateVelocity(rb, gravityDown, out Vector3 horizontalVelocity, out Vector3 _verticalVelocity);
        if (IsGrounded && !previouslyGrounded && velocityAlongGravity <= 0)
        {
            rb.linearVelocity = horizontalVelocity;
        }

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

    private void UpdatePlatformAudioParameters()
    {
        AudioManager.SurfaceParameter = (float)SurfaceType.Default;
        if (currentPlatformSurface != null)
        {
            AudioManager.SurfaceParameter = currentPlatformSurface.ParameterValue;
        }

        Debug.Log($"[PlayerController] Set surface audio parameter to {AudioManager.SurfaceParameter}");
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

        // Calculate movement direction relative to camera
        Vector3 moveDirection = Vector3.zero;
        if (cameraTransform != null && moveInput.magnitude > 0.01f)
        {
            // W = camera forward flattened onto surface plane
            // A/D = camera right flattened onto surface plane
            // This works on any surface orientation — flat, wall, sphere — without special cases
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, upDirection).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, upDirection).normalized;

            // Fallback: if camera is looking almost straight up/down the gravity axis
            // (e.g. directly above player), use player's own forward instead
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.ProjectOnPlane(transform.forward, upDirection).normalized;
            if (right.sqrMagnitude < 0.01f)
                right = Vector3.Cross(upDirection, forward).normalized;

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
        float verticalComponent = PhysicsUtils.VelocityIntoNormal(rb, gravityDirection);
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

        AudioManager.PlayOneShot(jumpEventReference, gameObject.transform.position);

        OnJumpSuccess?.Invoke();
    }

    // Called by playerAnimationController to sync with jump animation
    public void ApplyJumpForce()
    {
        Vector3 jumpDirection = gravityBody.GetUpDirection();

        // Strip downward velocity so jump goes upward right when landing
        Vector3 currentVelocity = rb.linearVelocity;
        float verticalComponent = PhysicsUtils.VelocityIntoNormal(rb, jumpDirection);
        if (verticalComponent < 0)
        {
            rb.linearVelocity = currentVelocity - (jumpDirection * verticalComponent);
        }

        rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
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

    private void TryPlayLandSound()
    {
        if (!CanPlayLandSound) return;

        landSFXCooldownTimer.Restart();
        AudioManager.PlayOneShotOnSurface(landEventReference, transform.position);
    }

    public float GetHorizontalSpeed()
    {
        PhysicsUtils.SeperateVelocity(
            rb,
            gravityBody.GetGravityDirection(),
            out Vector3 horizontalVelocity,
            out Vector3 _
        );
        return horizontalVelocity.magnitude;
    }

    public void RestoreBodyState(PlayerBodyState bodyState, bool restoreVelocity = false)
    {
        // Restore position and rotation
        transform.position = bodyState.Position;
        transform.rotation = bodyState.Rotation;

        // Restore gravity direction
        gravityBody.SetCustomGravityDirection(-bodyState.GravityDirection, rotateVelocity: false);

        // Restore velocity if desired
        if (restoreVelocity)
        {
            rb.linearVelocity = bodyState.LinearVelocity;
            rb.angularVelocity = bodyState.AngularVelocity;
        }
    }
}
