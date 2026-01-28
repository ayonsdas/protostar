using UnityEngine;

public class TestPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public float lookXLimit = 90f;

    private float xInput;
    private float yInput;
    private float rotationX = 0f;
    
    // We now track this based on the GameState, not a local toggle
    private bool isControlActive = false;

    public Camera playerCamera;

    void Start()
    {
        // Subscribe to state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
            
            // Initialize based on current state (in case we start directly InGame)
            OnGameStateChanged(GameStateManager.Instance.CurrentState);
        }
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }

    // This function automatically runs whenever the State Manager changes states
    private void OnGameStateChanged(GameStateManager.GameState newState)
    {
        if (newState == GameStateManager.GameState.InGame)
        {
            // Resume Game: Lock cursor and enable movement
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isControlActive = true;
        }
        else
        {
            // Menu/Paused: Unlock cursor and disable movement
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isControlActive = false;
        }
    }

    void Update()
    {
        // 1. Handle Pausing
        // If we are playing and hit Escape, tell the Manager to Pause.
        // The Manager will then fire the event to unlock the cursor.
        if (isControlActive && Input.GetKeyDown(KeyCode.Escape))
        {
            GameStateManager.Instance.SetState(GameStateManager.GameState.Paused);
            return; // Stop processing this frame
        }

        // 2. Stop here if we aren't allowed to move
        if (!isControlActive) return;

        // --- Movement Logic (Only runs if InGame) ---
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = transform.right * xInput + transform.forward * yInput;
        transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime, Space.World);

        // --- Mouse Look Logic (Only runs if InGame) ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        if (playerCamera)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
    }
}