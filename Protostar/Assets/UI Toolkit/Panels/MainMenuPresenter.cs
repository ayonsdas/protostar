using System;
using FMODUnity;
using UnityEngine.UIElements;

public class MainMenuPresenter
{
    public Action OpenSettings { set => UIHelper.RegisterButton(settingsButton, value, clickSound: _clickSound, hoverSound: _hoverSound); }
    public Action StartGame { set => UIHelper.RegisterButton(startButton, value, clickSound: _startSound, hoverSound: _hoverSound); }
    public Action QuitGame { set => UIHelper.RegisterButton(quitButton, value, clickSound: _clickSound, hoverSound: _hoverSound); }
    public Action OpenCredits { set => UIHelper.RegisterButton(creditsButton, value, clickSound: _clickSound, hoverSound: _hoverSound); }

    private Button startButton;
    private Button settingsButton;
    private Button creditsButton;
    private Button quitButton;

    private readonly EventReference _clickSound;
    private readonly EventReference _hoverSound;
    private readonly EventReference _startSound;

    public MainMenuPresenter(
        VisualElement root,
        EventReference clickSound = default,
        EventReference hoverSound = default,
        EventReference startSound = default
    )
    {
        startButton = root.Q<Button>("Start");
        settingsButton = root.Q<Button>("Settings");
        creditsButton = root.Q<Button>("Credits");
        quitButton = root.Q<Button>("Quit");

        _clickSound = clickSound;
        _hoverSound = hoverSound;
        _startSound = startSound;
    }
}