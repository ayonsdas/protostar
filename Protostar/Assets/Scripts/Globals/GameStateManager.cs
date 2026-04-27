using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public event Action<GameState> OnStateChanged;

    private Stack<GameState> previousStates;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        previousStates = new Stack<GameState>();
    }

    private void OnEnable()
    {
        if (!InputModeManager.HasPlayerInput)
        {
            Debug.LogWarning($"[PlayerController] Cannot find PlayerInput");
        }
        else
        {
            PlayerInput playerInput = InputModeManager.PlayerInput;
            playerInput.actions["ToggleMenu"].performed += OnToggleMenu;
            playerInput.actions["Cancel"].performed += OnCancel;
        }
    }

    private void OnDisable()
    {
        if (!InputModeManager.HasPlayerInput)
        {
            Debug.LogWarning($"[PlayerController] Cannot find PlayerInput");
        }
        else
        {
            PlayerInput playerInput = InputModeManager.PlayerInput;
            playerInput.actions["ToggleMenu"].performed -= OnToggleMenu;
            playerInput.actions["Cancel"].performed -= OnCancel;
        }
    }

    // Toggle menu when this action is pressed
    private void OnToggleMenu(InputAction.CallbackContext _ctx)
    {
        switch (CurrentState)
        {
            case GameState.MainMenu:
                break;

            case GameState.InGame:
            case GameState.Cutscene:
                SetState(GameState.Paused);
                break;

            // If in menus, exit if we were in game
            default:
                if (previousStates.Contains(GameState.InGame))
                {
                    GameState nextState = previousStates.Contains(GameState.Cutscene) ? GameState.Cutscene : GameState.InGame;
                    SetState(nextState);
                }
                break;
        }
    }

    // On back navigation, revert to previous state if changable
    private void OnCancel(InputAction.CallbackContext _ctx)
    {
        Debug.Log("Cancel");
        switch (CurrentState)
        {
            case GameState.Paused:
            case GameState.Settings:
            case GameState.Controls:
            case GameState.Credits:
                RevertState();
                break;
        }
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        // If going into game, close the UI, use player controls, lock cursor, etc
        if (newState == GameState.InGame)
        {
            CloseUI();
        }
        // If leaving in game state, open the UI, use UI controls, unlock cursor, etc
        else if (CurrentState == GameState.InGame || newState == GameState.Cutscene)
        {
            OpenUI();
        }

        // When returning to menu or game, clear menu navigation history
        if (newState == GameState.MainMenu || newState == GameState.InGame)
        {
            ClearPreviousStates();
        }

        // If we have this state in our history, remove all history up to that point.
        else if (previousStates.Contains(newState))
        {
            while (previousStates.Count > 0)
            {
                if (previousStates.Pop() == newState) break;
            }
        }
        // Otherwise, add it to the history
        else
        {
            previousStates.Push(CurrentState);
        }

        Debug.Log($"[GameStateManager] Set state to {newState} from {CurrentState}");
        foreach (var state in previousStates)
        {
            Debug.Log($"[GameStateManager] Previous state: {state}");
        }
        CurrentState = newState;

        // Pause/unpause game time
        bool timePaused = newState != GameState.InGame;
        Time.timeScale = timePaused ? 0f : 1f;

        try
        {
            bool musicPaused = newState != GameState.InGame && newState != GameState.Cutscene;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicParameter("IsPause", musicPaused ? 1f : 0f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GameStateManager] Failed to set music pause parameter: {e.Message}");
        }

        OnStateChanged?.Invoke(newState);
    }

    public void RevertState()
    {
        if (previousStates.Count > 0)
        {
            GameState previousState = previousStates.Peek();
            SetState(previousState);
        }
    }

    private void ClearPreviousStates()
    {
        previousStates.Clear();
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
    private void OpenUI()
    {
        Time.timeScale = 0f;
        InputModeManager.PlayerInput.actions.FindActionMap("Player").Disable();
        InputModeManager.PlayerInput.actions.FindActionMap("UI").Enable();
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    private void CloseUI()
    {
        Time.timeScale = 1f;
        InputModeManager.PlayerInput.actions.FindActionMap("UI").Disable();
        InputModeManager.PlayerInput.actions.FindActionMap("Player").Enable();
        Cursor.lockState = CursorLockMode.Locked;
    }
}