using System.Collections;
using UnityEngine;

public class Deadzone : MonoBehaviour
{
    public CheckpointSystem checkpointSystem;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            checkpointSystem.TryRespawnPlayer();
        }
    }
}