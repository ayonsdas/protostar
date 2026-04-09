using System;
using FMODUnity;
using UnityEngine.UIElements;

public static class UIHelper
{

    public const string CONTROLLER_MODE_CLASS = "controller-mode";
    public const string MOUSE_MODE_CLASS = "mouse-mode";

    public static void RegisterButton(
        Button button,
        Action action,
        EventReference clickSound = default,
        EventReference hoverSound = default
    )
    {
        if (button == null || action == null) return;

        // Add click sound if provided
        if (!clickSound.IsNull)
        {
            action += () => AudioManager.PlayOneShot(clickSound);
        }

        button.clicked -= action;
        button.clicked += action;

        if (!hoverSound.IsNull)
        {
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                AudioManager.PlayOneShot(hoverSound);
            });
        }
    }

    public static void SetControllerMode(VisualElement root)
    {
        // If already in controller mode, dont need to switch
        if (root.ClassListContains(CONTROLLER_MODE_CLASS)) return;

        //Debug.Log("Set to controller mode");
        root.RemoveFromClassList(MOUSE_MODE_CLASS);
        root.AddToClassList(CONTROLLER_MODE_CLASS);
    }

    public static void SetMouseMode(VisualElement root)
    {
        // If already in controller mode, dont need to switch
        if (root.ClassListContains(MOUSE_MODE_CLASS)) return;

        //Debug.Log("Set to mouse mode");
        root.RemoveFromClassList(CONTROLLER_MODE_CLASS);
        root.AddToClassList(MOUSE_MODE_CLASS);
    }
}
