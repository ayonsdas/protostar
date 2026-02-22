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

    private PlayerInput playerInput;
    public PlayerInput PlayerInput => playerInput;

    private const string PATTERN = @"{([A-Za-z0-9_]+)}";
    private static Regex regex = new Regex(PATTERN);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            playerInput = GetComponent<PlayerInput>();

            if (playerInput == null)
            {
                Debug.LogError("[InputManager] cannot find PlayerInput");
                return;
            }
            foreach(InputActionMap map in playerInput.actions.actionMaps)
            {
                map.Disable();
            }
            playerInput.actions.FindActionMap("Global").Enable();
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
            CurrentInputMode = inputMode;
            InputModeChanged?.Invoke(inputMode);
        }
    }

    public static string ReplaceBindings(string input)
    {
        PlayerInput playerInput = InputModeManager.Instance.PlayerInput;
        return Regex.Replace(input, PATTERN, match =>
        {
            string actionName = match.Groups[1].Value;
            InputAction action = playerInput.actions.FindAction(actionName);

            if (action == null)
            {
                return match.Value;
            }

            return Instance.GetActionDisplayString(action) ?? match.Value;
        });
    }

    public string GetActionDisplayString(InputAction action)
    {
        InputBinding bindingMask = InputBinding.MaskByGroup(playerInput.currentControlScheme);

        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];

            // Don't consider binding if not in active control scheme
            if (!binding.isComposite && !bindingMask.Matches(binding))
            {
                continue;
            }

            // Resolve composite bindings
            if (binding.isComposite)
            {
                return GetCompositeActionDisplayString(action, i);
            }
            else if (!binding.isPartOfComposite)
            {
                string display = action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
                if (!string.IsNullOrEmpty(display))
                {
                    return "[" + display + "]";
                }
            }
        }

        return null;
    }

    private string GetCompositeActionDisplayString(InputAction action, int i, int maxGroups = 1)
    {
        Dictionary<string, List<string>> parts = new();

        // Collect composite parts
        int partIndex = i + 1;
        while (
            partIndex < action.bindings.Count &&
            action.bindings[partIndex].isPartOfComposite
        )
        {
            var part = action.bindings[partIndex];
            //Debug.Log($"[ActionBindingText] Composite part {action.GetBindingDisplayString(partIndex)} name/composite part {part.name}");

            // Can maybe use InputBinding.DisplayStringOptions.DontUseShortDisplayNames, see if I like this result better
            string display = action.GetBindingDisplayString(partIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
            if (!string.IsNullOrEmpty(display))
            {
                if (!parts.ContainsKey(part.name))
                {
                    parts[part.name] = new List<string>();
                }
                parts[part.name].Add(display);
            }

            partIndex++;
        }

        // Group composite bindings into groups based on index like WASD, up, left, down, right, then combine these
        List<List<string>> combinedBindings = new List<List<string>>();
        foreach (List<string> compositeGroupBindings in parts.Values)
        {
            int compositeGroup = 0;
            foreach (string compositeGroupBinding in compositeGroupBindings)
            {
                if (combinedBindings.Count <= compositeGroup)
                {
                    combinedBindings.Add(new List<string> { compositeGroupBinding });
                }
                else
                {
                    combinedBindings[compositeGroup].Add(compositeGroupBinding);
                }

                compositeGroup++;
            }
        }

        List<string> compositeBindings = new List<string>();
        for (int group = 0; group < maxGroups; group++)
        {
            compositeBindings.Add("[" + string.Join(", ",  combinedBindings[group]) + "]");
        }
        return string.Join(" or ", compositeBindings);
    }
}