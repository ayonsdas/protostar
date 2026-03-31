using UnityEngine;

public class PlayableZone : MonoBehaviour
{
    public bool PlayerInside { get; private set; } = false;

    private CheckpointSystem checkpointSystem;

    private void Start()
    {
        checkpointSystem = FindFirstObjectByType<CheckpointSystem>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerInside = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerInside = false;
            checkpointSystem?.TryRespawnPlayer();
        }
    }
}
