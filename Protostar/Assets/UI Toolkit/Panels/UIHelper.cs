using System;
using UnityEngine.UIElements;

public static class UIHelper
{
    public static void RegisterButton(Button button, Action action)
    {
        if (button == null || action == null) return;
        button.clicked -= action;
        button.clicked += action;
    }
}
