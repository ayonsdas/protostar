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

    private const string PATTERN = @"{([A-Za-z0-9_]+)}";

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

    public static string ReplaceBindings(string input)
    {
        PlayerInput playerInput = PlayerInput;
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

    public Dictionary<string, List<string>> GetAllActionDisplayStrings(PlayerInput playerInput)
    {
        Dictionary<string, List<string>> res = new Dictionary<string, List<string>>();
        foreach (InputAction action in playerInput.actions)
        {
            Dictionary<string, List<string>> binds = GetActionDisplayDict(action);
            foreach ((string name, List<string> bind) in binds)
            {
                res[name] = bind;
            }
        }

        return res;
    }

    public Dictionary<string, List<string>> GetActionDisplayDict(InputAction action)
    {
        Dictionary<string, List<string>> res = new Dictionary<string, List<string>>();
        InputBinding bindingMask = InputBinding.MaskByGroup(PlayerInput.currentControlScheme);

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
                Dictionary<string, List<string>> composites = GetCompositeActionGroupDisplayStrings(action, i);
                foreach ((string part, List<string> binds) in composites)
                {
                    if (!res.ContainsKey(part))
                    {
                        res[part] = binds;
                    }
                    else
                    {
                        foreach (var bind in binds)
                        {
                            res[part].Add(bind);
                        }
                    }
                }
            }
            else if (!binding.isPartOfComposite)
            {
                string display = action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
                if (!string.IsNullOrEmpty(display))
                {
                    string bind = "[" + display + "]";
                    if (!res.ContainsKey(action.name))
                    {
                        res[action.name] = new List<string> { bind };
                    }
                    else
                    {
                        res[action.name].Add(bind);
                    }
                }
            }
        }

        return res;
    }

    public string GetActionDisplayString(InputAction action)
    {
        InputBinding bindingMask = InputBinding.MaskByGroup(PlayerInput.currentControlScheme);

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
        Dictionary<string, List<string>> parts = GetCompositeActionGroupDisplayStrings(action, i);

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
            compositeBindings.Add("[" + string.Join(", ", combinedBindings[group]) + "]");
        }
        return string.Join(" or ", compositeBindings);
    }

    private Dictionary<string, List<string>> GetCompositeActionGroupDisplayStrings(InputAction action, int i)
    {
        if (!action.bindings[i].isComposite)
        {
            Debug.LogError($"[InputModeManager] Action {action.name} binding index {i} is not composite");
            return null;
        }

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
                string key = $"{action.name} {part.name}";
                if (!parts.ContainsKey(key))
                {
                    parts[key] = new List<string>();
                }
                parts[key].Add(display);
            }

            partIndex++;
        }

        return parts;
    }
}