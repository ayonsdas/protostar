using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuPresenter
{
    public Action OpenSettings { set => settingsButton.clicked += value; }
    public Action StartGame { set => startButton.clicked += value; }
    public Action QuitGame { set => quitButton.clicked += value; }
    public Action OpenCredits { set => creditsButton.clicked += value; }
    
    private Button startButton;
    private Button settingsButton;
    private Button creditsButton;
    private Button quitButton;
    

    public MainMenuPresenter(VisualElement root)
    {
        startButton = root.Q<Button>("Start");
        settingsButton = root.Q<Button>("Settings");
        creditsButton = root.Q<Button>("Credits");
        quitButton = root.Q<Button>("Quit");
    }   
}