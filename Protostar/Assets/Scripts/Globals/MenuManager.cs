using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    private IMenuView activeView;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[MenuManager] Duplicate Menu manager, destroying");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
        InputModeManager.Instance.InputModeChanged += HandleInputModeChanged;
    }

    void OnDisable()
    {
        GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
        InputModeManager.Instance.InputModeChanged -= HandleInputModeChanged;
    }

    public void RegisterView(IMenuView view)
    {
        activeView = view;

        // Sync state immediately
        view.OnGameStateChanged(GameStateManager.Instance.CurrentState);
        view.OnInputModeChanged(InputModeManager.Instance.CurrentInputMode);
    }

    public void UnregisterView(IMenuView view)
    {
        if (activeView == view)
        {
            activeView = null;
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        activeView?.OnGameStateChanged(state);
    }

    private void HandleInputModeChanged(InputMode mode)
    {
        activeView?.OnInputModeChanged(mode);
    }
}