using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Assign the player in the Inspector

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 5f, -10f); // Position behind and above player
    public float smoothTime = 0.05f; // How smoothly camera follows (I set this to 0, it really doesn't work that well because camera jitters)
    
    [Header("Camera Collision")]
    public LayerMask collisionLayers; // Layers to check for obstacles
    public float collisionRadius = 0.3f; // Radius of the camera sphere for collision
    public float collisionSmoothTime = 0.1f; // How smoothly camera adjusts to obstacles
    public float minDistance = 0.5f; // Minimum distance camera can be from player
    
    private Vector3 velocity = Vector3.zero; // Used by SmoothDamp
    
    [Header("Camera Rotation Settings")]
    public float rotationSpeed = 100f;
    public float returnDelay = 3f; // Seconds before returning to default
    public float returnSpeed = 2f; // Speed of return to default

    private PlayerInput playerInput;
    private InputAction lookAction;
    private InputAction mouseHoldAction;
    private float horizontalAngle = 0f;
    private float verticalAngle = 0f;
    private float timeSinceLastInput = 0f;
    private bool isReturning = false;
    private float currentDistance = 0f;
    private float distanceVelocity = 0f;

    void Start()
    {
        if (target != null)
        {
            playerInput = target.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                lookAction = playerInput.actions["Look"];
                mouseHoldAction = playerInput.actions["MouseHold"];
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
        bool isMouseHeld = false;
        
        if (lookAction != null)
        {
            lookInput = lookAction.ReadValue<Vector2>();
        }
        
        if (mouseHoldAction != null)
        {
            isMouseHeld = mouseHoldAction.ReadValue<float>() > 0.5f;
        }

        // Check if there's camera input AND mouse is held
        if (lookInput.magnitude > 0.01f && isMouseHeld)
        {
            horizontalAngle += lookInput.x * rotationSpeed * Time.deltaTime;
            horizontalAngle = Mathf.Clamp(horizontalAngle, -180f, 180f); // Limit to 1.5 rotations
            verticalAngle -= lookInput.y * rotationSpeed * Time.deltaTime;
            verticalAngle = Mathf.Clamp(verticalAngle, -60f, 60f); // Limit vertical rotation - increased up range
            
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

        // Calculate camera rotation offset in local space
        Quaternion localRotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
        
        // Apply rotation to the base offset in local space
        Vector3 rotatedOffset = localRotation * offset;
        
        // Transform everything to world space using player's rotation
        Vector3 worldOffset = target.TransformDirection(rotatedOffset);
        Vector3 desiredPosition = target.position + worldOffset;
        
        // Check for obstacles between camera and player
        float desiredDistance = worldOffset.magnitude;
        Vector3 direction = worldOffset.normalized;
        float targetDistance = desiredDistance;
        
        if (collisionLayers.value != 0)
        {
            RaycastHit hit;
            if (Physics.SphereCast(target.position, collisionRadius, direction, out hit, desiredDistance, collisionLayers))
            {
                // Move camera to just past the hit point
                targetDistance = Mathf.Max(hit.distance - collisionRadius, minDistance);
            }
        }
        
        // Smoothly adjust distance
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, collisionSmoothTime);
        
        // Apply adjusted distance
        Vector3 adjustedPosition = target.position + direction * currentDistance;

        // Smoothly move camera
        transform.position = Vector3.SmoothDamp(transform.position, adjustedPosition, ref velocity, smoothTime);

        // Camera rotation should also be relative to player
        // The camera's forward should point at the player, but its up should align with player's up
        Vector3 directionToTarget = target.position - transform.position;
        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget, target.up);
            transform.rotation = lookRotation;
        }
    }
}
