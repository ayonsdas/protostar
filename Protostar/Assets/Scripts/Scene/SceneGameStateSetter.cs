using UnityEngine;

public class SceneGameStateSetter : MonoBehaviour
{
    [SerializeField] private GameState stateOnLoad;

    private void Awake()
    {
        GameStateManager.Instance.SetState(stateOnLoad);
    }
}