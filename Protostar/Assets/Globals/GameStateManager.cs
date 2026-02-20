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

    public enum GameState
    {
        MainMenu,
        InGame,
        Paused,   // Settings open while in-game
        Settings, // Settings from main menu
        Credits,  // Credits screen
        Controls  // Controls screen (from settings)
    }

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
        DontDestroyOnLoad(gameObject);
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

    public void SetState(GameState newState, bool reverting=false)
    {
        if (CurrentState == newState)
            return;

        // If resetting to base state, clear stored states, otherwise, add to previous states
        if(newState == GameState.InGame || newState == GameState.MainMenu)
        {
            ClearPreviousStates();
        }
        else if(!reverting)
        {
            previousStates.Push(CurrentState);
        }

        CurrentState = newState;
        
        // Pause/unpause game time
        Time.timeScale = (newState == GameState.InGame) ? 1f : 0f;
        
        OnStateChanged?.Invoke(newState);
    }

    public void RevertState()
    {
        if(previousStates != null && previousStates.Count > 0)
        {
            GameState previousState = previousStates.Pop();
            SetState(previousState, reverting: true);
        }
    }

    public void RevertToBaseState()
    {
        while(previousStates.Count > 0)
        {
            RevertState();
        }
    }

    private void ClearPreviousStates()
    {
        previousStates = new Stack<GameState>();
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