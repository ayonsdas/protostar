using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMode
{
    Mouse,
    Controller
}

public class InputModeManager : MonoBehaviour
{
    public static InputModeManager Instance { get; private set; }

    public InputMode CurrentInputMode { get; private set; } = InputMode.Mouse;
    public event Action<InputMode> InputModeChanged;

    [SerializeField] private PlayerInput playerInput;
    public PlayerInput PlayerInput { get { return playerInput; } }

    [Range(0f,1f)]
    [SerializeField] private float deadzone = 0.05f;

    [SerializeField] private InputActionReference navigateAction;
    [SerializeField] private InputActionReference pointAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        navigateAction.action.performed += OnNavigate;
        pointAction.action.performed += OnPoint;
    }

    void OnDisable()
    {
        navigateAction.action.performed -= OnNavigate;
        pointAction.action.performed -= OnPoint;
    }

    private void OnPoint(InputAction.CallbackContext _ctx)
    {
        SetInputMode(InputMode.Mouse);
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (!(ctx.control.device is Gamepad))
            return;

        // Check what action is, ignore stick drift
        Vector2 value = ctx.ReadValue<Vector2>();

        if (value.magnitude < deadzone) return;

        SetInputMode(InputMode.Controller);
    }

    private void SetInputMode(InputMode newMode)
    {
        if (CurrentInputMode == newMode)
            return;

        CurrentInputMode = newMode;
        InputModeChanged?.Invoke(newMode);
    }
}
