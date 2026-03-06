
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ActionBindingUtil
{
    private static PlayerInput playerInput = InputModeManager.PlayerInput;
    private const string PATTERN = @"{([A-Za-z0-9_]+)}";
    public static string ReplaceBindings(string input)
    {
        return Regex.Replace(input, PATTERN, match =>
        {
            string actionName = match.Groups[1].Value;
            InputAction action = playerInput.actions.FindAction(actionName);

            if (action == null)
            {
                return match.Value;
            }

            return GetActionDisplayString(action) ?? match.Value;
        });
    }

    public static Dictionary<string, List<string>> GetAllActionDisplayStrings(PlayerInput playerInput)
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

    public static Dictionary<string, List<string>> GetActionDisplayDict(InputAction action)
    {
        Dictionary<string, List<string>> res = new Dictionary<string, List<string>>();
        InputBinding bindingMask = InputBinding.MaskByGroup(InputModeManager.PlayerInput.currentControlScheme);

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

    public static string GetActionDisplayString(InputAction action)
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

    private static string GetCompositeActionDisplayString(InputAction action, int i, int maxGroups = 1)
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

    private static Dictionary<string, List<string>> GetCompositeActionGroupDisplayStrings(InputAction action, int i)
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