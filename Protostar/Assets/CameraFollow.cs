using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Assign the player in the Inspector

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 5f, -10f); // Position behind and above player
    public float smoothTime = 0.05f; // How smoothly camera follows (I set this to 0, it really doesn't work that well because camera jitters)
    
    private Vector3 velocity = Vector3.zero; // Used by SmoothDamp
    
    [Header("Camera Rotation Settings")]
    public float rotationSpeed = 100f;
    public float returnDelay = 3f; // Seconds before returning to default
    public float returnSpeed = 2f; // Speed of return to default

    private PlayerInput playerInput;
    private InputAction lookAction;
    private float horizontalAngle = 0f;
    private float verticalAngle = 0f;
    private float timeSinceLastInput = 0f;
    private bool isReturning = false;

    void Start()
    {
        if (target != null)
        {
            playerInput = target.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                lookAction = playerInput.actions["Look"];
            }
            
            // Enable interpolation on the player's Rigidbody to reduce jitter
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Get camera rotation input
        Vector2 lookInput = Vector2.zero;
        if (lookAction != null)
        {
            lookInput = lookAction.ReadValue<Vector2>();
        }

        // Check if there's camera input
        if (lookInput.magnitude > 0.01f)
        {
            horizontalAngle += lookInput.x * rotationSpeed * Time.deltaTime;
            verticalAngle -= lookInput.y * rotationSpeed * Time.deltaTime;
            verticalAngle = Mathf.Clamp(verticalAngle, -30f, 60f); // Limit vertical rotation
            
            timeSinceLastInput = 0f;
            isReturning = false;
        }
        else
        {
            timeSinceLastInput += Time.deltaTime;
            
            // Start returning to default after delay
            if (timeSinceLastInput >= returnDelay)
            {
                isReturning = true;
            }
        }

        // Smoothly return to default rotation
        if (isReturning)
        {
            horizontalAngle = Mathf.Lerp(horizontalAngle, 0f, returnSpeed * Time.deltaTime);
            verticalAngle = Mathf.Lerp(verticalAngle, 0f, returnSpeed * Time.deltaTime);
            
            // Stop returning when close enough
            if (Mathf.Abs(horizontalAngle) < 0.1f && Mathf.Abs(verticalAngle) < 0.1f)
            {
                horizontalAngle = 0f;
                verticalAngle = 0f;
                isReturning = false;
            }
        }

        // Calculate rotation based on player's forward direction + camera angles
        Quaternion rotation = target.rotation * Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + rotation * offset;

        // Use SmoothDamp for jitter-free camera following
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // Always look at the player
        transform.LookAt(target);
    }
}
