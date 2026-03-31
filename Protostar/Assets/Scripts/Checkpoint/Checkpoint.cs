using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public CheckpointSystem checkpointSystem;
    [Tooltip("Checkpoint 1 should start with 1")]
    public int checkpointNumber = 1;
    private bool activated = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && !activated)
        {
            activated = true;
            checkpointSystem.SetActiveCheckpoint(checkpointNumber);
        }
    }
}
