using UnityEngine;
using UnityEngine.InputSystem;

public class Telescope : MonoBehaviour, IInteractable
{
    [Header("Telescope Settings")]
    [SerializeField] private Camera telescopeCamera;
    [SerializeField] private Transform cameraRoot; // Empty GameObject to rotate (pivot point)
    [SerializeField] private GameObject telescopeHead; // Visual model that rotates around cameraRoot
    [SerializeField] private Light telescopeLight; // Light that rotates around cameraRoot
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, 0); // Offset from cameraRoot to camera
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float verticalLimit = 60f; // Max angle up/down
    
    private Vector3 headOffset;
    private Vector3 lightOffset;
    private Quaternion headLocalRotation;
    private Quaternion lightLocalRotation;
    
    [Header("Requirements")]
    [SerializeField] private SaplingPuzzle requiredPuzzle; // Must complete this puzzle before using telescope
    
    [Header("Target Detection")]
    [SerializeField] private GameObject targetObject; // The red sphere to look for
    [SerializeField] private float detectionRange = 1000f;
    [SerializeField] private float alignmentThreshold = 0.98f; // How accurate aim needs to be (0.98 = ~11 degrees)
    [SerializeField] private Color targetColor = Color.red;
    
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
        
        // Disable telescope light by default
        if (telescopeLight != null)
        {
            telescopeLight.enabled = false;
        }
        
        // Calculate initial offset if cameraRoot is set
        if (cameraRoot != null && telescopeCamera != null)
        {
            cameraOffset = cameraRoot.InverseTransformPoint(telescopeCamera.transform.position);
        }
        
        // Calculate head offset and rotation
        if (cameraRoot != null && telescopeHead != null)
        {
            headOffset = cameraRoot.InverseTransformPoint(telescopeHead.transform.position);
            headLocalRotation = Quaternion.Inverse(cameraRoot.rotation) * telescopeHead.transform.rotation;
        }
        
        // Calculate light offset and rotation
        if (cameraRoot != null && telescopeLight != null)
        {
            lightOffset = cameraRoot.InverseTransformPoint(telescopeLight.transform.position);
            lightLocalRotation = Quaternion.Inverse(cameraRoot.rotation) * telescopeLight.transform.rotation;
        }
    }
    
    void Update()
    {
        if (isActive)
        {
            HandleTelescopeRotation();
            CheckTargetAlignment();
        }
    }
    
    /// <summary>
    /// Check if the telescope light is currently on (puzzle complete indicator)
    /// </summary>
    public bool IsLightOn()
    {
        return telescopeLight != null && telescopeLight.enabled;
    }
    
    private void CheckTargetAlignment()
    {
        if (telescopeCamera == null || targetObject == null || telescopeLight == null)
            return;
        
        // Get direction from telescope to target
        Vector3 directionToTarget = (targetObject.transform.position - telescopeCamera.transform.position).normalized;
        Vector3 telescopeForward = telescopeCamera.transform.forward;
        
        // Check if telescope is pointing at target
        float alignment = Vector3.Dot(telescopeForward, directionToTarget);
        
        Debug.Log($"Alignment: {alignment:F3} (threshold: {alignmentThreshold:F3})");
        
        if (alignment >= alignmentThreshold)
        {
            // Pointing at target - turn light on and make it red
            telescopeLight.enabled = true;
            telescopeLight.color = targetColor;
            Debug.Log("TARGET ALIGNED - Light ON");
        }
        else
        {
            // Not pointing at target - turn light off
            telescopeLight.enabled = false;
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
            
            // Position and rotate telescope head
            if (telescopeHead != null)
            {
                Vector3 worldHeadOffset = cameraRoot.TransformDirection(headOffset);
                telescopeHead.transform.position = cameraRoot.position + worldHeadOffset;
                telescopeHead.transform.rotation = cameraRoot.rotation * headLocalRotation;
            }
            
            // Position and rotate telescope light
            if (telescopeLight != null)
            {
                Vector3 worldLightOffset = cameraRoot.TransformDirection(lightOffset);
                telescopeLight.transform.position = cameraRoot.position + worldLightOffset;
                telescopeLight.transform.rotation = cameraRoot.rotation * lightLocalRotation;
                Debug.Log($"Light moved to: {telescopeLight.transform.position}, rotation: {telescopeLight.transform.rotation.eulerAngles}");
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
        // Check if puzzle requirement is met
        if (requiredPuzzle != null && requiredPuzzle.GetState() == 0)
        {
            Debug.Log("The telescope is locked. Complete the sapling puzzle first!");
            return;
        }
        
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
        isActive = false;
        
        // Re-enable player camera
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            
            // Re-enable camera follow script
            if (cameraFollow != null)
            {
                cameraFollow.enabled = true;
            }
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
