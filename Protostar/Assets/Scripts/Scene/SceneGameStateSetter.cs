using static GameStateManager;
using UnityEngine;

public class SceneGameStateSetter : MonoBehaviour
{
    [SerializeField] private GameState stateOnLoad;

    private void Start()
    {
        GameStateManager.Instance.SetState(stateOnLoad);
    }
}