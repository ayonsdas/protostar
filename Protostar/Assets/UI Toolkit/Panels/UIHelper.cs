using System;
using FMODUnity;
using UnityEngine.UIElements;

public static class UIHelper
{
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
}
