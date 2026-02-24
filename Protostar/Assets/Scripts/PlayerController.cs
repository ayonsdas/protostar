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

    private Rigidbody rb;
    private CustomGravityBody gravityBody;
    private Vector2 moveInput;
    private bool isGrounded;
    private Quaternion targetRotation;
    private EventInstance footstepEventInstance;
    private float footstepStartTime;
    private Transform cameraTransform;
    private bool movementLocked = false;

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

        // If no groundCheck transform is assigned, create one at the bottom of the collider
        if (groundCheck == null)
        {
            GameObject checkObj = new GameObject("GroundCheck");
            checkObj.transform.parent = transform;

            // Get box collider height to position ground check at the very bottom
            BoxCollider box = GetComponent<BoxCollider>();
            float colliderBottom = box != null ? -(box.size.y / 2f) + 0.1f : -0.9f;

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
        }
    }

    void Update()
    {
        // Get gravity direction for proper orientation
        Vector3 gravityDown = gravityBody.GetGravityDirection();

        // Use OverlapSphere to check for ground, excluding player's own collider
        Collider[] colliders = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
        isGrounded = false;

        // Check if any of the overlapping colliders are NOT the player's own collider
        Collider playerCollider = GetComponent<Collider>();
        foreach (Collider col in colliders)
        {
            if (col != playerCollider)
            {
                isGrounded = true;
                break;
            }
        }

        // Debug visualization using gravity direction
        Color debugColor = isGrounded ? Color.green : Color.red;
        Debug.DrawLine(groundCheck.position, groundCheck.position + gravityDown * groundCheckRadius, debugColor);

        // Play sound if grounded and moving
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
        }

        // Calculate movement direction relative to camera or gravity
        Vector3 moveDirection = Vector3.zero;
        if (cameraTransform != null && moveInput.magnitude > 0.01f)
        {
            Vector3 upDirection = gravityBody.GetUpDirection();
            // If gravity is normal, use camera-relative movement as before
            // Always calculate camera-relative movement
            // Always calculate camera-relative movement, but project onto gravity plane first
            // Calculate the rotation from natural gravity (Vector3.down) to current gravity
            Quaternion gravityDelta = Quaternion.FromToRotation(Vector3.down, -upDirection);
            // Rotate the camera's orientation by this delta
            Vector3 rotatedForward = gravityDelta * cameraTransform.forward;
            Vector3 rotatedRight = gravityDelta * cameraTransform.right;
            // Project onto the plane perpendicular to gravity
            rotatedForward = Vector3.ProjectOnPlane(rotatedForward, upDirection).normalized;
            rotatedRight = Vector3.ProjectOnPlane(rotatedRight, upDirection).normalized;
            Vector3 camMove = (rotatedForward * moveInput.y + rotatedRight * moveInput.x);
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
        if (isGrounded && rb != null && ctx.performed)
        {
            // Jump in the opposite direction of gravity
            Vector3 jumpDirection = gravityBody.GetUpDirection();
            rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
            AudioManager.Instance.PlayOneShot(jumpEventReference, gameObject.transform.position);
        }
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

    private bool canStartFootsteps()
    {
        bool moving = rb.linearVelocity.magnitude >= footstepSpeedThreshold;
        bool debounce = Time.time - footstepStartTime > footstepDebounceTime;
        return moving && debounce && isGrounded;
    }

    private void UpdateSound()
    {
        if (disableFootsteps) return;
        if (canStartFootsteps())
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

        //else
        //{
        //    PLAYBACK_STATE playbackState;
        //    footstepEventInstance.getPlaybackState(out playbackState);
        //    if (playbackState == PLAYBACK_STATE.PLAYING)
        //    {
        //        //Debug.Log("Stopped footsteps Velocity: " + rb.linearVelocity + " Grounded: " + isGrounded);
        //        footstepEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        //    }

        //}
    }
}
