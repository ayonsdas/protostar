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

        // Subscribe to state changes
        GameStateManager.Instance.OnStateChanged += OnGameStateChanged;

        // Initialize UI based on current state
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
        
        // Back button - return to settings or paused state
        controlsPresenter.BackAction = () =>
        {
            var previousState = GameStateManager.Instance.GetPreviousState();
            if (previousState == GameStateManager.GameState.Paused)
            {
                GameStateManager.Instance.SetState(GameStateManager.GameState.Paused);
            }
            else
            {
                GameStateManager.Instance.SetState(GameStateManager.GameState.Settings);
            }
        };
    }

    private void SetupSettingsMenu()
    {
        SettingsPresenter settingsPresenter = new SettingsPresenter(root.Q<TemplateContainer>("Settings"));
        
        // Back button - return to previous screen
        settingsPresenter.BackAction = () =>
        {
            var currentState = GameStateManager.Instance.CurrentState;
            if (currentState == GameStateManager.GameState.Settings)
            {
                // Came from main menu
                GameStateManager.Instance.SetState(GameStateManager.GameState.MainMenu);
            }
            else if (currentState == GameStateManager.GameState.Paused)
            {
                // Came from in-game, resume playing
                GameStateManager.Instance.SetState(GameStateManager.GameState.InGame);
            }
        };
        
        // Return to main menu button (used when paused in-game)
        settingsPresenter.ReturnToMainMenuAction = () =>
        {
            GameStateManager.Instance.ReturnToMainMenu(mainMenuSceneName);
        };
        
        // Controls button - open controls page
        settingsPresenter.ControlsAction = () =>
        {
            GameStateManager.Instance.SetState(GameStateManager.GameState.Controls);
        };
    }

    private void SetupCreditsMenu()
    {
        CreditsPresenter creditsPresenter = new CreditsPresenter(root.Q<TemplateContainer>("Credits"));
        creditsPresenter.BackAction = () => GameStateManager.Instance.SetState(GameStateManager.GameState.MainMenu);
    }

    private void OnGameStateChanged(GameStateManager.GameState newState)
    {
        // Show main menu only in MainMenu state
        mainMenuView.Display(newState == GameStateManager.GameState.MainMenu);
        
        // Show settings in both Settings (from menu) and Paused (from game) states
        settingsView.Display(newState == GameStateManager.GameState.Settings || 
                            newState == GameStateManager.GameState.Paused);
        
        // Show credits only in Credits state
        creditsView.Display(newState == GameStateManager.GameState.Credits);
        
        // Show controls only in Controls state
        controlsView.Display(newState == GameStateManager.GameState.Controls);
        
        // Show root UI except when actively playing
        root.Display(newState != GameStateManager.GameState.InGame);
    }
}