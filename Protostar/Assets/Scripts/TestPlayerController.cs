using UnityEngine;
// using UnityEngine.InputSystem; // Note: We are using the standard Input Manager, so this isn't needed.

public class TestPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public float lookXLimit = 90f; // Prevents head from spinning 360 degrees

    float xInput;
    float yInput;
    float rotationX = 0f;

    public Camera playerCamera;

    void Start()
    {
        // Lock the mouse to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- 1. Movement Input ---
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        // Calculate movement direction relative to where we are looking
        Vector3 moveDirection = transform.right * xInput + transform.forward * yInput;
        
        // Apply Movement
        transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime, Space.World);

        // --- 2. Mouse Look Input ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate the BODY Left/Right (Y-Axis)
        transform.Rotate(Vector3.up * mouseX);

        // Rotate the CAMERA Up/Down (X-Axis)
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        
        // Apply rotation to camera
        if (playerCamera)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
    }

    void FixedUpdate() 
    {
        // Since we are using transform.Translate in Update (non-physics movement),
        // we can leave this empty. 
        // If you were using a Rigidbody, you would apply force/velocity here.
    }
}