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
    public LayerMask groundLayer = -1; // Default to everything

    [Header("Gravity Rotation Settings")]
    public float gravityRotationSpeed = 2f; // How fast player rotates to match gravity

    private Rigidbody rb;
    private CustomGravityBody gravityBody;
    private Vector2 moveInput;
    private bool isGrounded;
    private Quaternion targetRotation;

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

    void Update()
    {
        // Get the "down" direction based on current gravity
        Vector3 gravityDown = gravityBody.GetGravityDirection();
        
        // Position ground check at the bottom of the player in gravity's direction
        BoxCollider box = GetComponent<BoxCollider>();
        float checkDistance = box != null ? (box.size.y / 2f) : 1f;
        Vector3 checkPosition = transform.position + gravityDown * checkDistance;
        
        // Use CheckSphere for ground detection
        isGrounded = Physics.CheckSphere(checkPosition, groundCheckRadius, groundLayer);
        
        // Debug visualization
        Color debugColor = isGrounded ? Color.green : Color.red;
        Debug.DrawRay(checkPosition, gravityDown * 0.5f, debugColor);
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
        // Calculate movement direction relative to current rotation
        Vector3 moveDirection = transform.forward * moveInput.y;
        
        // Apply movement using Rigidbody for smooth physics-based movement
        Vector3 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        // Apply rotation using Rigidbody - rotate around gravity's up direction
        if (moveInput.x != 0)
        {
            Vector3 upDirection = gravityBody.GetUpDirection();
            Quaternion deltaRotation = Quaternion.AngleAxis(moveInput.x * turnSpeed * Time.fixedDeltaTime, upDirection);
            rb.MoveRotation(deltaRotation * rb.rotation);
        }
        
        // Smoothly align player to gravity direction (after turning so it doesn't override)
        AlignToGravity();
    }

    // Called by Player Input component (Send Messages behavior)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
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
}
