using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public enum GameState
    {
        MainMenu,
        InGame,
        Paused,  // Settings open while in-game
        Settings // Settings from main menu
    }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public event Action<GameState> OnStateChanged;

    private GameState previousState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // ESC key toggles settings/pause when in-game
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.InGame)
            {
                SetState(GameState.Paused);
            }
            else if (CurrentState == GameState.Paused)
            {
                SetState(GameState.InGame);
            }
        }
    }

    public void SetState(GameState newState)
    {
        previousState = CurrentState;
        CurrentState = newState;
        
        // Pause/unpause game time
        Time.timeScale = (newState == GameState.InGame) ? 1f : 0f;
        
        OnStateChanged?.Invoke(newState);
    }

    public GameState GetPreviousState()
    {
        return previousState;
    }

    public void StartGame(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        SetState(GameState.InGame);
    }

    public void ReturnToMainMenu(string mainMenuSceneName = "MainMenu")
    {
        SceneManager.LoadScene(mainMenuSceneName);
        SetState(GameState.MainMenu);
    }
}