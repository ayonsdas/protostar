using FMODUnity;
using UnityEngine;
using UnityEngine.UIElements;

public class StartViewPresenter : MonoBehaviour, IMenuView
{


    [SerializeField] private string gameSceneName = "MainLevel";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Button SFX")]
    [SerializeField] private EventReference clickSound;
    [SerializeField] private EventReference hoverSound;
    [SerializeField] private EventReference startSound;

    private VisualElement root;
    private VisualElement settingsView;
    private VisualElement mainMenuView;
    private VisualElement creditsView;
    private VisualElement controlsView;

    void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        settingsView = root.Q<TemplateContainer>("Settings");
        mainMenuView = root.Q<TemplateContainer>("MainMenu");
        creditsView = root.Q<TemplateContainer>("Credits");
        controlsView = root.Q<TemplateContainer>("Controls");
    }

    void Start()
    {
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
        if (MenuManager.Instance)
        {
            MenuManager.Instance.RegisterView(this);
        }
        else
        {
            Debug.LogWarning("[StartViewPresenter] cannot find MenuManger to register with");
        }
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
                UIHelper.SetMouseMode(root);
                break;
            case InputMode.Controller:
                UIHelper.SetControllerMode(root);
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

    private void SetupMainMenu()
    {
        MainMenuPresenter menuPresenter = new MainMenuPresenter(
            mainMenuView,
            clickSound: clickSound,
            hoverSound: hoverSound,
            startSound: startSound
        );
        menuPresenter.OpenSettings = () => GameStateManager.Instance.SetState(GameState.Settings);
        menuPresenter.StartGame = () => GameStateManager.Instance.StartGame(gameSceneName);
        menuPresenter.OpenCredits = () => GameStateManager.Instance.SetState(GameState.Credits);
        menuPresenter.QuitGame = () => Application.Quit();
    }

    private void SetupControlsMenu()
    {
        ControlsPresenter controlsPresenter = new ControlsPresenter(
            root.Q<TemplateContainer>("Controls"),
            clickSound: clickSound,
            hoverSound: hoverSound
        );
        controlsPresenter.BackAction = () => GameStateManager.Instance.RevertState();
    }

    private void SetupSettingsMenu()
    {
        SettingsPresenter settingsPresenter = new SettingsPresenter(
            root.Q<TemplateContainer>("Settings"),
            clickSound: clickSound,
            hoverSound: hoverSound
        );
        settingsPresenter.BackAction = () => GameStateManager.Instance.RevertState();
        settingsPresenter.ReturnToMainMenuAction = () => GameStateManager.Instance.ReturnToMainMenu(mainMenuSceneName);
        settingsPresenter.ControlsAction = () => GameStateManager.Instance.SetState(GameState.Controls);
    }

    private void SetupCreditsMenu()
    {
        CreditsPresenter creditsPresenter = new CreditsPresenter(
            root.Q<TemplateContainer>("Credits"),
            clickSound: clickSound,
            hoverSound: hoverSound
        );
        creditsPresenter.BackAction = () => GameStateManager.Instance.RevertState();
    }

    private void SetButtonFocus(GameState state)
    {

        // Set initial focus for controller navigation
        Button focusTarget = state switch
        {
            GameState.MainMenu => mainMenuView.Q<Button>(),
            GameState.Settings or GameState.Paused => settingsView.Q<Button>(),
            GameState.Credits => creditsView.Q<Button>(),
            GameState.Controls => controlsView.Q<Button>(),
            _ => null
        };

        if (focusTarget == null) return;

        focusTarget.Focus();
    }
}