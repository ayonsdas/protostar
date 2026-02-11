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
    
    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;
    public bool requireMouseHold = true; // If true, must hold mouse button to look

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
    [SerializeField] private EventReference footstepEventReference;

    private Rigidbody rb;
    private CustomGravityBody gravityBody;
    private Vector2 moveInput;
    private Vector2 mouseDelta;
    private bool isMouseHeld = false;
    private bool isGrounded;
    private Quaternion targetRotation;
    private EventInstance footstepEventInstance;
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
        
        // Calculate movement direction relative to camera
        Vector3 moveDirection = Vector3.zero;
        
        if (cameraTransform != null && moveInput.magnitude > 0.01f)
        {
            // Get camera's forward and right directions
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            
            // Project camera directions onto the plane perpendicular to gravity
            Vector3 upDirection = gravityBody.GetUpDirection();
            cameraForward = Vector3.ProjectOnPlane(cameraForward, upDirection).normalized;
            cameraRight = Vector3.ProjectOnPlane(cameraRight, upDirection).normalized;
            
            // Calculate movement direction based on input
            // For pure A or D input (x axis only), use 90-degree angle from camera
            if (Mathf.Abs(moveInput.x) > 0.01f && Mathf.Abs(moveInput.y) < 0.01f)
            {
                // Pure strafe left or right (90 degrees from camera forward)
                moveDirection = cameraRight * Mathf.Sign(moveInput.x);
            }
            else
            {
                // Combined input or forward/back - calculate normally
                moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
            }
            
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

        // Reset mouse delta each frame so it doesn't persist
        mouseDelta = Vector2.zero;
    }

    // Called by Player Input component (Send Messages behavior)
    public void OnMove(InputValue value)
    {
        if (!movementLocked)
        {
            moveInput = value.Get<Vector2>();
        }
    }
    
    // Called by Player Input component for mouse delta
    public void OnLook(InputValue value)
    {
        // Only accept mouse input if not requiring hold, or if mouse is held
        if (!requireMouseHold || isMouseHeld)
        {
            mouseDelta = value.Get<Vector2>();
        }
        else
        {
            mouseDelta = Vector2.zero;
        }
    }
    
    // Called when mouse button is pressed/released
    public void OnMouseHold(InputValue value)
    {
        isMouseHeld = value.isPressed;
        Debug.Log($"[MouseHold] isPressed: {value.isPressed}, isMouseHeld: {isMouseHeld}");
        
        // Lock/unlock cursor based on mouse hold state
        if (isMouseHeld)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Called by Player Input component (Send Messages behavior)
    public void OnJump(InputValue value)
    {
        if (isGrounded && rb != null && value.isPressed)
        {
            // Jump in the opposite direction of gravity
            Vector3 jumpDirection = gravityBody.GetUpDirection();
            rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
        }
    }

    // Called by Player Input component (Send Messages behavior)
    public void OnRotateGravity(InputValue value)
    {
        Debug.Log($"OnRotateGravity called! isPressed: {value.isPressed}");

        if (value.isPressed && GravityController.Instance != null)
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

    private void UpdateSound()
    {
        if (disableFootsteps) return;
        if (rb.linearVelocity.magnitude >= footstepSpeedThreshold && isGrounded)
        {
            PLAYBACK_STATE playbackState;
            footstepEventInstance.getPlaybackState(out playbackState);
            if (playbackState == PLAYBACK_STATE.STOPPED)
            {
                //Debug.Log("Started footsteps Velocity: " + rb.linearVelocity + " Grounded: " + isGrounded);
                footstepEventInstance.start();
            }
        }

        else
        {
            PLAYBACK_STATE playbackState;
            footstepEventInstance.getPlaybackState(out playbackState);
            if (playbackState == PLAYBACK_STATE.PLAYING)
            {
                //Debug.Log("Stopped footsteps Velocity: " + rb.linearVelocity + " Grounded: " + isGrounded);
                footstepEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }

        }
    }
}
