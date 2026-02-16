using System;
using UnityEngine.UIElements;

public class MainMenuPresenter
{
    public Action OpenSettings { set => UIHelper.RegisterButton(settingsButton, value); }
    public Action StartGame { set => UIHelper.RegisterButton(startButton, value); }
    public Action QuitGame { set => UIHelper.RegisterButton(quitButton, value); }
    public Action OpenCredits { set => UIHelper.RegisterButton(creditsButton, value); }
    
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