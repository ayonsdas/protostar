using System;
using UnityEngine;
using UnityEngine.UIElements;

public class StartViewPresenter : MonoBehaviour
{
    public static StartViewPresenter Instance { get; private set; }
    
    [SerializeField] private string gameSceneName = "MainLevel";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    private VisualElement root;
    private VisualElement settingsView;
    private VisualElement mainMenuView;
    private VisualElement creditsView;
    private VisualElement controlsView;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        settingsView = root.Q<TemplateContainer>("Settings");
        mainMenuView = root.Q<TemplateContainer>("MainMenu");
        creditsView = root.Q<TemplateContainer>("Credits");
        controlsView = root.Q<TemplateContainer>("Controls");

        SetupMainMenu();
        SetupSettingsMenu();
        SetupCreditsMenu();
        SetupControlsMenu();

        GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        OnGameStateChanged(GameStateManager.Instance.CurrentState);
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }

    private void SetupMainMenu()
    {
        MainMenuPresenter menuPresenter = new MainMenuPresenter(mainMenuView);
        menuPresenter.OpenSettings = () => GameStateManager.Instance.SetState(GameStateManager.GameState.Settings);
        menuPresenter.StartGame = () => GameStateManager.Instance.StartGame(gameSceneName);
        menuPresenter.OpenCredits = () => GameStateManager.Instance.SetState(GameStateManager.GameState.Credits);
        menuPresenter.QuitGame = () => Application.Quit();
    }

    private void SetupControlsMenu()
    {
        ControlsPresenter controlsPresenter = new ControlsPresenter(root.Q<TemplateContainer>("Controls"));
        controlsPresenter.BackAction = () =>
        {
            var previousState = GameStateManager.Instance.GetPreviousState();
            if (previousState == GameStateManager.GameState.Paused)
                GameStateManager.Instance.SetState(GameStateManager.GameState.Paused);
            else
                GameStateManager.Instance.SetState(GameStateManager.GameState.Settings);
        };
    }

    private void SetupSettingsMenu()
    {
        SettingsPresenter settingsPresenter = new SettingsPresenter(root.Q<TemplateContainer>("Settings"));
        settingsPresenter.BackAction = () =>
        {
            var currentState = GameStateManager.Instance.CurrentState;
            if (currentState == GameStateManager.GameState.Settings)
                GameStateManager.Instance.SetState(GameStateManager.GameState.MainMenu);
            else if (currentState == GameStateManager.GameState.Paused)
                GameStateManager.Instance.SetState(GameStateManager.GameState.InGame);
        };
        settingsPresenter.ReturnToMainMenuAction = () => GameStateManager.Instance.ReturnToMainMenu(mainMenuSceneName);
        settingsPresenter.ControlsAction = () => GameStateManager.Instance.SetState(GameStateManager.GameState.Controls);
    }

    private void SetupCreditsMenu()
    {
        CreditsPresenter creditsPresenter = new CreditsPresenter(root.Q<TemplateContainer>("Credits"));
        creditsPresenter.BackAction = () => GameStateManager.Instance.SetState(GameStateManager.GameState.MainMenu);
    }

    private void OnGameStateChanged(GameStateManager.GameState newState)
    {
        mainMenuView.Display(newState == GameStateManager.GameState.MainMenu);
        settingsView.Display(newState == GameStateManager.GameState.Settings || 
                            newState == GameStateManager.GameState.Paused);
        creditsView.Display(newState == GameStateManager.GameState.Credits);
        controlsView.Display(newState == GameStateManager.GameState.Controls);
        root.Display(newState != GameStateManager.GameState.InGame);

        // Set initial focus for controller navigation
        switch (newState)
        {
            case GameStateManager.GameState.MainMenu:
                mainMenuView.Q<Button>()?.Focus();
                break;
            case GameStateManager.GameState.Settings:
            case GameStateManager.GameState.Paused:
                settingsView.Q<Button>()?.Focus();
                break;
            case GameStateManager.GameState.Credits:
                creditsView.Q<Button>()?.Focus();
                break;
            case GameStateManager.GameState.Controls:
                controlsView.Q<Button>()?.Focus();
                break;
        }
    }
}