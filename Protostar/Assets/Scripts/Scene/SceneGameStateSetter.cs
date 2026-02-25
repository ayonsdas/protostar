using static GameStateManager;
using UnityEngine;

public class SceneGameStateSetter : MonoBehaviour
{
    [SerializeField] private GameState stateOnLoad;

    private void Start()
    {
        if (GameStateManager.Instance.CurrentState == GameState.MainMenu)
        {
            GameStateManager.Instance.SetState(stateOnLoad);
        }
    }
}