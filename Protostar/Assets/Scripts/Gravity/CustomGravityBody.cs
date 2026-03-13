using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityBody : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Range(1, 5)]
    [SerializeField] private float jumpReleasedGravityMultiplier = 2.5f;
    [Range(1, 5)]
    [SerializeField] private float fallingGravityMultiplier = 2f;
    private Rigidbody rb;
    private Vector3? customGravityDirection = null; // If set, uses this instead of global gravity
    private float gravityStrength = 100f; // Default strength
    private bool jumpHeld = false;

    [Header("Sound Settings")]
    [SerializeField] private float soundAngleThreshold = 150f;
    [SerializeField] private EventReference gravityFlipEvent;

    private void OnEnable()
    {
        if (!InputModeManager.HasPlayerInput)
        {
            Debug.LogWarning($"[CustomGravityBody] Cannot find PlayerInput");
        }
        else
        {
            PlayerInput playerInput = InputModeManager.PlayerInput;
            playerInput.actions["Jump"].performed += OnJump;
            playerInput.actions["Jump"].canceled += OnJump;
        }
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            jumpHeld = true;
        }
        else if (ctx.canceled)
        {
            jumpHeld = false;
        }
    }

    private void OnDisable()
    {
        if (!InputModeManager.HasPlayerInput)
        {
            Debug.LogWarning($"[CustomGravityBody] Cannot find PlayerInput");
        }
        else
        {
            PlayerInput playerInput = InputModeManager.PlayerInput;
            playerInput.actions["Jump"].performed -= OnJump;
            playerInput.actions["Jump"].canceled -= OnJump;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Disable Unity's built-in gravity so we can apply our own
        rb.useGravity = false;

        // Get gravity strength from controller if available
        if (GravityController.Instance != null)
        {
            gravityStrength = GravityController.Instance.GetGravity().magnitude;
        }
    }

    void FixedUpdate()
    {
        Vector3 gravityDirection = Vector3.zero;
        // If we have a custom gravity direction, use that
        if (customGravityDirection.HasValue)
        {
            gravityDirection = customGravityDirection.Value.normalized;
        }
        // Otherwise use global gravity
        else if (GravityController.Instance != null)
        {
            // Apply custom gravity force
            gravityDirection = GravityController.Instance.GetGravity().normalized;
        }
        else
        {
            Debug.LogError($"[CustomGravityBody] cannot find gravity source");
        }

        // detect if falling
        float verticalVelocity = Vector3.Dot(rb.linearVelocity, -gravityDirection);

        float gravityMultiplier = 1f;

        bool rising = verticalVelocity > 0;
        bool falling = verticalVelocity < 0;

        // choose gravity multiplier
        if (falling)
        {
            gravityMultiplier = fallingGravityMultiplier;
        }
        else if (rising && !jumpHeld)
        {
            gravityMultiplier = jumpReleasedGravityMultiplier;
        }

        // apply gravity
        Vector3 gravity = gravityDirection * gravityStrength * gravityMultiplier;
        rb.AddForce(gravity, ForceMode.Acceleration);
    }

    /// <summary>
    /// Set a custom gravity direction for this object (independent of global gravity)
    /// </summary>
    public void SetCustomGravityDirection(Vector3 direction, bool rotateVelocity = false)
    {
        Vector3 oldUpDirection = GetUpDirection();
        customGravityDirection = direction.normalized;

        Vector3 newUpDirection = GetUpDirection();
        float angle = Vector3.Angle(oldUpDirection, newUpDirection);

        if (angle > soundAngleThreshold)
        {
            if (AudioManager.Instance != null && !gravityFlipEvent.IsNull)
            {
                AudioManager.Instance.PlayOneShot(gravityFlipEvent, transform.position);
            }
        }

        if (rotateVelocity)
        {
            // Rotate the velocity to match the new gravity direction
            Quaternion gravityRotation = Quaternion.FromToRotation(oldUpDirection, newUpDirection);
            rb.linearVelocity = gravityRotation * rb.linearVelocity;
        }
    }

    /// <summary>
    /// Clear custom gravity and use global gravity again
    /// </summary>
    public void ClearCustomGravity()
    {
        customGravityDirection = null;
    }

    /// <summary>
    /// Check if this object has custom gravity set
    /// </summary>
    public bool HasCustomGravity()
    {
        return customGravityDirection.HasValue;
    }

    public Vector3 GetGravityDirection()
    {
        // If we have custom gravity, return that
        if (customGravityDirection.HasValue)
        {
            return customGravityDirection.Value;
        }

        // Otherwise return global gravity direction
        if (GravityController.Instance != null)
        {
            return GravityController.Instance.GetGravity().normalized;
        }
        return Vector3.down;
    }

    public Vector3 GetUpDirection()
    {
        // "Up" is opposite to gravity
        return -GetGravityDirection();
    }
}
