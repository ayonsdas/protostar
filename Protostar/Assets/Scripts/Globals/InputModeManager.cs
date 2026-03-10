using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputModeManager : MonoBehaviour
{
    public static InputModeManager Instance { get; private set; }

    public InputMode CurrentInputMode { get; private set; } = InputMode.Mouse;
    public event Action<InputMode> InputModeChanged;

    public static PlayerInput PlayerInput { get; private set; }

    public static bool HasPlayerInput => Instance != null && PlayerInput != null;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            PlayerInput = GetComponent<PlayerInput>();

            if (PlayerInput == null)
            {
                Debug.LogError("[InputManager] cannot find PlayerInput");
                return;
            }
            foreach (InputActionMap map in PlayerInput.actions.actionMaps)
            {
                map.Disable();
            }
            PlayerInput.actions.FindActionMap("Global").Enable();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        PlayerInput.onControlsChanged += HandleControlsChanged;
    }

    void OnDisable()
    {
        PlayerInput.onControlsChanged -= HandleControlsChanged;
    }

    private void HandleControlsChanged(PlayerInput _playerInput)
    {
        Debug.Log($"[InputModeManager] Controls changed!");
        InputMode inputMode;
        switch (PlayerInput.currentControlScheme)
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

        if (inputMode != CurrentInputMode)
        {
            Debug.Log($"[InputModeManager] Set input mode to {inputMode}");
            CurrentInputMode = inputMode;
            InputModeChanged?.Invoke(inputMode);
        }
    }
}