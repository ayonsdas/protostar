using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.UIElements;

public class ControlsPresenter
{
    private Button returnButton;
    private readonly EventReference _clickSound;
    private readonly EventReference _hoverSound;

    public Action BackAction { set { UIHelper.RegisterButton(returnButton, value, clickSound: _clickSound, hoverSound: _hoverSound); } }

    public ControlsPresenter(
        VisualElement root,
        EventReference clickSound = default,
        EventReference hoverSound = default
    )
    {
        returnButton = root.Q<Button>("Return");
        if (returnButton == null)
        {
            Debug.LogError("Return button not found in Controls view");
        }

        _clickSound = clickSound;
        _hoverSound = hoverSound;
    }
}