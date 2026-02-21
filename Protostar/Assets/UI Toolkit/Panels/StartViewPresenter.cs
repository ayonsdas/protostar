using UnityEngine;
using UnityEngine.UIElements;

public class StartViewPresenter : MonoBehaviour, IMenuView
{
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
        root = GetComponent<UIDocument>().rootVisualElement;
    }

    void Start()
    {
        settingsView = root.Q<TemplateContainer>("Settings");
        mainMenuView = root.Q<TemplateContainer>("MainMenu");
        creditsView = root.Q<TemplateContainer>("Credits");
        controlsView = root.Q<TemplateContainer>("Controls");

        SetupMainMenu();
        SetupSettingsMenu();
        SetupCreditsMenu();
        SetupControlsMenu();

        // Set initial modes
        OnGameStateChanged(GameStateManager.Instance.CurrentState);
        OnInputModeChanged(InputModeManager.Instance.CurrentInputMode);
    }

    private void OnEnable()
    {
        MenuManager.Instance.RegisterView(this);
    }

    private void OnDisable()
    {
        if (MenuManager.Instance != null)
            MenuManager.Instance.UnregisterView(this);
    }

    public void OnInputModeChanged(InputMode inputMode)
    {
        switch (inputMode)
        {
            case InputMode.Mouse:
                root.focusController.focusedElement?.Blur();
                SetMouseMode();
                break;
            case InputMode.Controller:
                SetControllerMode();
                SetButtonFocus(GameStateManager.Instance.CurrentState);
                break;
        }
    }
    public void OnGameStateChanged(GameState newState)
    {
        mainMenuView.Display(newState == GameState.MainMenu);
        settingsView.Display(newState == GameState.Settings ||
                            newState == GameState.Paused);
        creditsView.Display(newState == GameState.Credits);
        controlsView.Display(newState == GameState.Controls);
        root.Display(newState != GameState.InGame);
        if (InputModeManager.Instance.CurrentInputMode == InputMode.Controller)
        {
            SetButtonFocus(newState);
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
        menuPresenter.OpenSettings = () => GameStateManager.Instance.SetState(GameState.Settings);
        menuPresenter.StartGame = () => GameStateManager.Instance.StartGame(gameSceneName);
        menuPresenter.OpenCredits = () => GameStateManager.Instance.SetState(GameState.Credits);
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
        settingsPresenter.ControlsAction = () => GameStateManager.Instance.SetState(GameState.Controls);
    }

    private void SetupCreditsMenu()
    {
        CreditsPresenter creditsPresenter = new CreditsPresenter(root.Q<TemplateContainer>("Credits"));
        creditsPresenter.BackAction = () => GameStateManager.Instance.RevertState();
    }

    private void SetButtonFocus(GameState state)
    {

        // Set initial focus for controller navigation
        switch (state)
        {
            case GameState.MainMenu:
                mainMenuView.Q<Button>()?.Focus();
                break;
            case GameState.Settings:
            case GameState.Paused:
                settingsView.Q<Button>()?.Focus();
                break;
            case GameState.Credits:
                creditsView.Q<Button>()?.Focus();
                break;
            case GameState.Controls:
                controlsView.Q<Button>()?.Focus();
                break;
        }
    }
}