using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

public class StartViewPresenter : MonoBehaviour
{
    public static StartViewPresenter Instance { get; private set; }

    private const string CONTROLLER_MODE_CLASS = "controller-mode";
    private const string MOUSE_MODE_CLASS = "mouse-mode";

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
            Debug.Log("Duplicate Start View, Destroying");
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

        var button = mainMenuView.Q<Button>();

        // Force initial focus after UI has fully initialized
        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            button.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            root.panel?.visualTree.Focus();
            button.Focus();

            //Debug.Log("Focused after layout: " + root.panel?.focusController?.focusedElement);
        }

        button.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        // Set initial modes
        OnGameStateChanged(GameStateManager.Instance.CurrentState);
        OnInputModeChanged(InputModeManager.Instance.CurrentInputMode);
    }

    private void OnEnable()
    {
        Debug.Log("Start View Enabled");
        GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        InputModeManager.Instance.InputModeChanged += OnInputModeChanged;
    }

    private void OnDisable()
    {
        Debug.Log("Start View Disabled");
        GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        InputModeManager.Instance.InputModeChanged -= OnInputModeChanged;
    }

    private void OnInputModeChanged(InputMode inputMode)
    {
        switch (inputMode)
        {
            case InputMode.Mouse:
                SetMouseMode();
                break;
            case InputMode.Controller:
                SetControllerMode();
                break;
        }
    }

    private void SetControllerMode()
    {
        // If already in controller mode, dont need to switch
        if(root.ClassListContains(CONTROLLER_MODE_CLASS)) return;

        //Debug.Log("Set to controller mode");
        root.RemoveFromClassList(MOUSE_MODE_CLASS);
        root.AddToClassList(CONTROLLER_MODE_CLASS);
    }

    private void SetMouseMode()
    {
        // If already in controller mode, dont need to switch
        if (root.ClassListContains(MOUSE_MODE_CLASS)) return;

        //Debug.Log("Set to mouse mode");
        root.RemoveFromClassList(CONTROLLER_MODE_CLASS);
        root.AddToClassList(MOUSE_MODE_CLASS);
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
        controlsPresenter.BackAction = () => GameStateManager.Instance.RevertState();
    }

    private void SetupSettingsMenu()
    {
        SettingsPresenter settingsPresenter = new SettingsPresenter(root.Q<TemplateContainer>("Settings"));
        settingsPresenter.BackAction = () => GameStateManager.Instance.RevertState();
        settingsPresenter.ReturnToMainMenuAction = () => GameStateManager.Instance.ReturnToMainMenu(mainMenuSceneName);
        settingsPresenter.ControlsAction = () => GameStateManager.Instance.SetState(GameStateManager.GameState.Controls);
    }

    private void SetupCreditsMenu()
    {
        CreditsPresenter creditsPresenter = new CreditsPresenter(root.Q<TemplateContainer>("Credits"));
        creditsPresenter.BackAction = () => GameStateManager.Instance.RevertState();
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