using System;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    private readonly List<IMenuView> activeViews = new();
    private readonly List<object> cutscenePlayers = new();

    private readonly Dictionary<object, List<Action>> _cutsceneCloseCallbacks = new();
    private readonly List<Action> _currentCutsceneCloseCallbacks = new();

    private ICutscenePlayer activeCutscenePlayer;
    private object activeCutscene;

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

    public void RegisterCutscenePlayer<T>(ICutscenePlayer<T> player)
    {
        IMenuView playerMenu = player as IMenuView;
        if (playerMenu == null) return;

        cutscenePlayers.Add(player);
        RegisterView(playerMenu);
    }

    public void UnregisterCutscenePlayer<T>(ICutscenePlayer<T> player)
    {
        IMenuView playerMenu = player as IMenuView;
        if (playerMenu == null) return;

        if (player == activeCutscenePlayer)
        {
            activeCutscene = null;
            activeCutscenePlayer = null;
        }

        cutscenePlayers.Remove(player);
        UnregisterView(playerMenu);
    }

    public void RegisterView(IMenuView view)
    {
        activeViews.Add(view);

        // Sync state immediately
        view.OnGameStateChanged(GameStateManager.Instance.CurrentState);
        view.OnInputModeChanged(InputModeManager.Instance.CurrentInputMode);
    }

    public void UnregisterView(IMenuView view)
    {
        activeViews.Remove(view);
    }

    public void PlayCutscene<T>(T cutscene)
    {
        // Dont play multiple cutscenes at once
        if (activeCutscene != null)
        {
            Debug.LogWarning($"[MenuManager] already playing cutscene {activeCutscene}, cannot start new cutscene {cutscene}");
            return;
        }

        foreach (var player in cutscenePlayers)
        {
            if (player is ICutscenePlayer<T> typedPlayer)
            {
                typedPlayer.Play(cutscene);
                activeCutscene = cutscene;
                activeCutscenePlayer = typedPlayer;
                activeCutscenePlayer.OnClose += HandleCutsceneClose;
                return;
            }
        }

        Debug.LogWarning($"[MenuManager] No cutscene player found for type {typeof(T).Name}");
    }

    public void AddCurrentCutsceneCloseCallback(Action callback)
    {
        _currentCutsceneCloseCallbacks.Add(callback);
    }

    public void AddCutsceneCloseCallback<T>(T cutscene, Action callback)
    {
        if (!_cutsceneCloseCallbacks.ContainsKey(cutscene))
            _cutsceneCloseCallbacks[cutscene] = new List<Action>();

        _cutsceneCloseCallbacks[cutscene].Add(callback);
    }

    private void HandleCutsceneClose()
    {
        activeCutscenePlayer.OnClose -= HandleCutsceneClose;

        object closedCutscene = activeCutscene;

        activeCutscenePlayer = null;
        activeCutscene = null;

        // Run all callbacks for this specific cutscene
        if (_cutsceneCloseCallbacks.TryGetValue(closedCutscene, out var callbacks))
        {
            foreach (var callback in callbacks)
            {
                callback();
            }
            // Maybe should not remove if we want after every instance?
            _cutsceneCloseCallbacks.Remove(closedCutscene);
        }

        // Run all callbacks for the currnet cutscene
        foreach (var callback in _currentCutsceneCloseCallbacks)
        {
            callback();
        }
        _currentCutsceneCloseCallbacks.Clear();
    }

    private void HandleGameStateChanged(GameState state)
    {
        foreach (var view in activeViews)
        {
            view.OnGameStateChanged(state);
        }
    }

    private void HandleInputModeChanged(InputMode mode)
    {
        foreach (var view in activeViews)
        {
            view.OnInputModeChanged(mode);
        }
    }
}