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

        SetupMainMenu();
        SetupSettingsMenu();

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
        menuPresenter.QuitGame = () => Application.Quit();
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
    }

    private void OnGameStateChanged(GameStateManager.GameState newState)
    {
        // Show main menu only in MainMenu state
        mainMenuView.Display(newState == GameStateManager.GameState.MainMenu);
        
        // Show settings in both Settings (from menu) and Paused (from game) states
        settingsView.Display(newState == GameStateManager.GameState.Settings || 
                            newState == GameStateManager.GameState.Paused);
        
        // Show root UI except when actively playing
        root.Display(newState != GameStateManager.GameState.InGame);
    }
}