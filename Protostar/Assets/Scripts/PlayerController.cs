using System;
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

    [Header("Jump Settings")]
    public float jumpForce = 5f;
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

    private bool jumpQueued;
    private bool jumpCommitted;
    private Vector2 moveInput;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    public event Action OnJumpRequested;
    public float GetNormalizedSpeed => Mathf.Clamp(rb.linearVelocity.magnitude / moveSpeed, 0f, 1f);
    private Rigidbody rb;
    private CustomGravityBody gravityBody;
    private Quaternion targetRotation;
    private EventInstance footstepEventInstance;
    private float footstepStartTime;
    private float lastLandTime;
    private Transform cameraTransform;
    private CameraFollow cameraFollow;
    private bool movementLocked = false;

    [Header("Ground/Gravity Check Collider")]
    [Tooltip("Assign the BoxCollider used for ground and gravity checks. Only this collider will be used for detection.")]
    public BoxCollider groundCheckBoxCollider;

    private bool CanLand => Time.time > lastLandTime + landCooldownTime;

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

        // Initialize target rotation to current rotation
        targetRotation = transform.rotation;

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
    }

    void Start()
    {
        footstepEventInstance = AudioManager.Instance.CreateEventInstance(footstepEventReference);
        footstepStartTime = Time.time;

        // Get reference to main camera
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
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

        if (IsGrounded && !jumpQueued)
        {
            jumpCommitted = false;
        }

        // Debug visualization
        Color debugColor = IsGrounded ? Color.green : Color.red;
        Debug.DrawLine(groundCheck.position, groundCheck.position + gravityDown * groundCheckRadius, debugColor);

        UpdateSound();
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
            playerInput.actions["Move"].performed += OnMove;
            playerInput.actions["Move"].canceled += OnMove;
            playerInput.actions["Jump"].performed += OnJump;
            playerInput.actions["RotateGravity"].performed += OnRotateGravity;
        }
    }

    private void OnDisable()
    {
        if (InputModeManager.Instance == null || InputModeManager.Instance.PlayerInput == null)
        {
            Debug.LogWarning($"[PlayerController] Cannot find PlayerInput {InputModeManager.Instance}");
        }
        else
        {
            PlayerInput playerInput = InputModeManager.Instance.PlayerInput;
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
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        // Calculate movement direction relative to camera or gravity
        Vector3 moveDirection = Vector3.zero;
        if (cameraTransform != null && moveInput.magnitude > 0.01f)
        {
            Vector3 upDirection = gravityBody.GetUpDirection();

            // Check if camera is flipped (upside down)
            bool isCameraFlipped = cameraFollow != null && cameraFollow.IsCameraFlipped();

            // Calculate the rotation from natural gravity to current gravity
            // This allows smooth movement up walls/curves - holding forward maintains physical direction
            // When upside down, we calculate from Vector3.up instead of Vector3.down
            Vector3 baseGravity = isCameraFlipped ? Vector3.up : Vector3.down;
            Quaternion gravityDelta = Quaternion.FromToRotation(baseGravity, -upDirection);

            // Rotate the camera's orientation by this delta to get movement in gravity space
            Vector3 rotatedForward = gravityDelta * cameraTransform.forward;
            Vector3 rotatedRight = gravityDelta * cameraTransform.right;

            // Project onto the plane perpendicular to gravity
            rotatedForward = Vector3.ProjectOnPlane(rotatedForward, upDirection).normalized;
            rotatedRight = Vector3.ProjectOnPlane(rotatedRight, upDirection).normalized;

            // Calculate movement direction from input
            Vector3 camMove = rotatedForward * moveInput.y + rotatedRight * moveInput.x;
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

        // Calculate desired velocity
        Vector3 desiredVelocity = moveDirection * moveSpeed;

        // Use linearVelocity for proper collision detection
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 gravityDirection = gravityBody.GetGravityDirection();

        // Keep the vertical (gravity-aligned) component of velocity
        float verticalComponent = Vector3.Dot(currentVelocity, gravityDirection);
        Vector3 verticalVelocity = gravityDirection * verticalComponent;

        // Apply new velocity (horizontal movement + vertical velocity from gravity/jump)
        rb.linearVelocity = desiredVelocity + verticalVelocity;

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
        if (!IsGrounded || !ctx.performed) return;
        OnJumpRequested?.Invoke();
        ApplyJumpForce();
    }

    // Called by playerAnimationController to sync with jump animation
    public void ApplyJumpForce()
    {
        // Jump in the opposite direction of gravity
        Vector3 jumpDirection = gravityBody.GetUpDirection();
        rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
        jumpQueued = false;
        AudioManager.Instance.PlayOneShot(jumpEventReference, gameObject.transform.position);
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
        bool justLanded = Time.time <= lastLandTime + footstepDebounceTime;
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
        lastLandTime = Time.time;
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
