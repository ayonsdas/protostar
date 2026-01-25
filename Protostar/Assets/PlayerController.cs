using UnityEngine;
using UnityEngine.InputSystem;

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

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // If no groundCheck transform is assigned, create one at the bottom of the capsule
        if (groundCheck == null)
        {
            GameObject checkObj = new GameObject("GroundCheck");
            checkObj.transform.parent = transform;
            
            // Get capsule height to position ground check at the very bottom
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            float capsuleBottom = capsule != null ? -(capsule.height / 2f) : -1f;
            
            checkObj.transform.localPosition = new Vector3(0, capsuleBottom + 0.1f, 0); // Slightly above bottom
            groundCheck = checkObj.transform;
        }
    }

    void Update()
    {
        // Check if grounded using sphere cast from bottom of player
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Debug visualization and detailed info
        Color debugColor = isGrounded ? Color.green : Color.red;
        Debug.DrawRay(groundCheck.position, Vector3.down * 0.5f, debugColor);
        
        // Check what we're colliding with
        Collider[] colliders = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (colliders.Length > 0)
        {
            Debug.Log($"Ground check detecting {colliders.Length} colliders: {string.Join(", ", System.Array.ConvertAll(colliders, c => c.gameObject.name))}");
        }
    }

    void FixedUpdate()
    {
        // Calculate movement direction relative to current rotation
        Vector3 moveDirection = transform.forward * moveInput.y;
        
        // Apply movement using Rigidbody for smooth physics-based movement
        Vector3 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        // Apply rotation using Rigidbody
        if (moveInput.x != 0)
        {
            Quaternion deltaRotation = Quaternion.Euler(Vector3.up * moveInput.x * turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }

    // Called by Player Input component (Send Messages behavior)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Called by Player Input component (Send Messages behavior)
    public void OnJump(InputValue value)
    {
        Debug.Log($"Jump triggered! isPressed: {value.isPressed}, isGrounded: {isGrounded}, rb: {(rb != null ? "exists" : "null")}");
        
        if (isGrounded && rb != null && value.isPressed)
        {
            Debug.Log("Jumping!");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else
        {
            if (!isGrounded) Debug.Log("Not grounded - groundCheck position: " + groundCheck.position);
            if (rb == null) Debug.Log("Rigidbody is null");
            if (!value.isPressed) Debug.Log("Button not pressed");
        }
    }
}
