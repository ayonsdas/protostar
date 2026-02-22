using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] private InputActionReference toggleMenu;
    [SerializeField] private InputActionReference cancel;

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
        toggleMenu.action.performed += OnToggleMenu;
        cancel.action.performed += OnCancel;
    }

    private void OnDisable()
    {
        toggleMenu.action.performed -= OnToggleMenu;
        cancel.action.performed -= OnCancel;
    }

    // Toggle menu when this action is pressed
    private void OnToggleMenu(InputAction.CallbackContext _ctx)
    {
        switch(CurrentState)
        {
            case GameState.InGame:
                SetState(GameState.Paused);
                break;

            // If in menus, exit if we were in game
            case GameState.Paused:
            case GameState.Settings:
            case GameState.Controls:
            case GameState.Credits:
                //Debug.Log("Previous States " + previousStates);
                if (previousStates.ToArray()[previousStates.Count - 1] == GameState.InGame)
                {
                    SetState(GameState.InGame);
                }
                break;
        }
    }

    // On back navigation, revert to previous state if changable
    private void OnCancel(InputAction.CallbackContext _ctx)
    {
        Debug.Log("Cancel");
        switch(CurrentState)
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

        // When returning to menu or game, clear menu navigation history
        if(newState == GameState.MainMenu || newState == GameState.InGame)
        {
            ClearPreviousStates();
        }

        // If we have this state in our history, remove all history up to that point.
        else if(previousStates.Contains(newState))
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
        foreach(var state in previousStates)
        {
            Debug.Log($"[GameStateManager] Previous state: {state}");
        }
        CurrentState = newState;
        
        // Pause/unpause game time
        Time.timeScale = (newState == GameState.InGame) ? 1f : 0f;
        
        OnStateChanged?.Invoke(newState);
    }

    public void RevertState()
    {
        if(previousStates.Count > 0)
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
}