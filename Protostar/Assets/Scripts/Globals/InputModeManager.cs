using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputModeManager : MonoBehaviour
{
    public static InputModeManager Instance { get; private set; }

    public InputMode CurrentInputMode { get; private set; } = InputMode.Mouse;
    public event Action<InputMode> InputModeChanged;

    private PlayerInput playerInput;
    public PlayerInput PlayerInput => playerInput;

    [Range(0f, 1f)]
    [SerializeField] private float deadzone = 0.05f;

    [SerializeField] private InputActionReference navigateAction;
    [SerializeField] private InputActionReference pointAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("[InputManager] cannot find PlayerInput");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        playerInput.onControlsChanged += HandleControlsChanged;
    }

    void OnDisable()
    {
        playerInput.onControlsChanged -= HandleControlsChanged;
    }


    private void HandleControlsChanged(PlayerInput _playerInput)
    {
        InputMode inputMode;
        switch (playerInput.currentControlScheme)
        {
            case "Keyboard&Mouse":
                
                inputMode = InputMode.Mouse;
                break;
            case "Gamepad":
                inputMode = InputMode.Controller;
                break;
            default:
                return;
        }

        if (inputMode != CurrentInputMode) {
            Debug.Log($"[InputModeManager] Set input mode to {inputMode}");
            InputModeChanged?.Invoke(inputMode);
        }
    }
}