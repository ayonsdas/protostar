using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuPresenter
{
    public Action OpenSettings { set => settingsButton.clicked += value; }
    public Action StartGame { set => startButton.clicked += value; }
    public Action QuitGame { set => quitButton.clicked += value; }
    
    private Button startButton;
    private Button settingsButton;
    private Button quitButton;
    

    public MainMenuPresenter(VisualElement root)
    {
        startButton = root.Q<Button>("Start");
        settingsButton = root.Q<Button>("Settings");
        quitButton = root.Q<Button>("Quit");
    }   
}