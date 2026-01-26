using UnityEngine;
using UnityEngine.InputSystem;

public class Telescope : MonoBehaviour, IInteractable
{
    [Header("Telescope Settings")]
    [SerializeField] private Camera telescopeCamera;
    [SerializeField] private Transform cameraRoot; // Empty GameObject to rotate (pivot point)
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, 0); // Offset from cameraRoot to camera
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float verticalLimit = 60f; // Max angle up/down
    
    private bool isActive = false;
    private Camera playerCamera;
    private CameraFollow cameraFollow;
    private float currentHorizontalAngle = 0f;
    private float currentVerticalAngle = 0f;
    
    void Start()
    {
        // Disable telescope camera by default
        if (telescopeCamera != null)
        {
            telescopeCamera.enabled = false;
        }
        
        // Calculate initial offset if cameraRoot is set
        if (cameraRoot != null && telescopeCamera != null)
        {
            cameraOffset = cameraRoot.InverseTransformPoint(telescopeCamera.transform.position);
        }
    }
    
    void Update()
    {
        if (isActive)
        {
            HandleTelescopeRotation();
        }
    }
    
    private void HandleTelescopeRotation()
    {
        // Get arrow key input using new Input System
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        float horizontal = 0f;
        float vertical = 0f;
        
        if (keyboard.leftArrowKey.isPressed) horizontal = -1f;
        if (keyboard.rightArrowKey.isPressed) horizontal = 1f;
        if (keyboard.upArrowKey.isPressed) vertical = 1f;
        if (keyboard.downArrowKey.isPressed) vertical = -1f;
        
        // Update angles
        currentHorizontalAngle += horizontal * rotationSpeed * Time.deltaTime;
        currentVerticalAngle += vertical * rotationSpeed * Time.deltaTime;
        
        // Clamp vertical angle
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, -verticalLimit, verticalLimit);
        
        // Apply rotation to camera root
        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0f);
            
            // Position camera based on cameraRoot and offset
            if (telescopeCamera != null)
            {
                Vector3 worldOffset = cameraRoot.TransformDirection(cameraOffset);
                telescopeCamera.transform.position = cameraRoot.position + worldOffset;
                // Camera looks outward from root in the direction of the offset
                if (worldOffset.sqrMagnitude > 0.001f)
                {
                    telescopeCamera.transform.rotation = Quaternion.LookRotation(worldOffset.normalized, cameraRoot.up);
                }
                else
                {
                    telescopeCamera.transform.rotation = cameraRoot.rotation;
                }
            }
        }
        // If no root, rotate camera directly
        else if (telescopeCamera != null)
        {
            telescopeCamera.transform.localRotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0f);
        }
    }
    
    public void Interact(GameObject interactor)
    {
        if (!isActive)
        {
            // Enter telescope view
            EnterTelescopeView(interactor);
        }
        else
        {
            // Exit telescope view
            ExitTelescopeView();
        }
    }
    
    private void EnterTelescopeView(GameObject interactor)
    {
        isActive = true;
        
        // Find and disable player camera
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            playerCamera.enabled = false;
            
            // Disable camera follow script
            cameraFollow = playerCamera.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.enabled = false;
            }
        }
        
        // Enable telescope camera
        if (telescopeCamera != null)
        {
            telescopeCamera.enabled = true;
        }
        
        // Disable player movement
        PlayerController playerController = interactor.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        Debug.Log("Entered telescope view. Use arrow keys to rotate. Press F to exit.");
    }
    
    private void ExitTelescopeView()
    {    
            // Re-enable camera follow script
            if (cameraFollow != null)
            {
                cameraFollow.enabled = true;
            }
        
        isActive = false;
        
        // Re-enable player camera
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
        }
        
        // Disable telescope camera (but rotation persists)
        if (telescopeCamera != null)
        {
            telescopeCamera.enabled = false;
        }
        
        // Re-enable player movement
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }
        }
        
        Debug.Log("Exited telescope view.");
    }
}
