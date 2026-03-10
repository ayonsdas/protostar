using UnityEngine;

public class OverlayUIController : MonoBehaviour
{
    [Tooltip("Parent GameObject of all UI overlays in the scene")]
    [SerializeField] private GameObject overlays;
    private void OnEnable()
    {
        if (GameStateManager.Instance)
            GameStateManager.Instance.OnStateChanged += HandleStateChanged;
    }
    private void OnDisable()
    {
        if (GameStateManager.Instance)
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.InGame)
        {
            overlays.SetActive(true);
        }
        else
        {
            overlays.SetActive(false);
        }
    }
}
