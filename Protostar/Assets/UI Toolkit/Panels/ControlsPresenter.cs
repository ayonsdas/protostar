using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ControlsPresenter
{
    private Button returnButton;
    public Action BackAction { set { if (returnButton != null) returnButton.clicked += value; } }

    public ControlsPresenter(VisualElement root)
    {
        returnButton = root.Q<Button>("Return");
        if (returnButton == null)
        {
            Debug.LogError("Return button not found in Controls view");
        }
    }
}
