using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.UIElements;

public class CreditsPresenter
{
    private Button returnButton;
    public Action BackAction { set => UIHelper.RegisterButton(returnButton, value, clickSound: _clickSound, hoverSound: _hoverSound); }

    private readonly EventReference _clickSound;
    private readonly EventReference _hoverSound;

    public CreditsPresenter(
        VisualElement root,
        EventReference clickSound = default,
        EventReference hoverSound = default
    )
    {
        returnButton = root.Q<Button>("Return");

        _clickSound = clickSound;
        _hoverSound = hoverSound;
    }
}