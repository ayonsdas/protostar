using System.Collections;
using UnityEngine;

public class CheckpointSystem : MonoBehaviour
{
    [Tooltip("Index 0 should be set to the starting spawn point.")]
    public Transform[] spawnPoints;
    public int activeCheckpointIndex = 0;
    [Tooltip("This should be set to the starting spawn point by default.")]
    public Transform activeSpawnPoint;

    [Header("Respawn Variables")]
    public Transform playerTransform;
    private Quaternion playerStartRot;
    private Vector3 playerStartPos;
    public PlayerController playerController;
    public Rigidbody playerRB;
    public float respawnTime = 2f;
    public bool teleport = false;

    void Start()
    {
        activeCheckpointIndex = 0;

        playerStartPos = playerTransform.position + Vector3.up;
        playerStartRot = playerTransform.rotation;
    }

    public void SetActiveCheckpoint(int checkpointNumber)
    {
        if(checkpointNumber > activeCheckpointIndex)
        {
            activeCheckpointIndex = checkpointNumber;
            activeSpawnPoint = spawnPoints[activeCheckpointIndex];
            Debug.Log("Active Checkpoint updated to Checkpoint " + activeCheckpointIndex);
        }
    }

    public void RespawnPlayer()
    {
        playerController.SetMovementLocked(true);
        playerRB.useGravity = false;
        playerRB.linearVelocity = Vector3.zero;
        playerRB.angularVelocity = Vector3.zero;

        if(teleport == false)
        {
            StartCoroutine(MovePlayerToCheckpoint());
        }  
        else
        {
            playerTransform.position = activeSpawnPoint.position;
            playerTransform.rotation = activeSpawnPoint.rotation;
            playerController.SetMovementLocked(false);
        }  
    }

    IEnumerator MovePlayerToCheckpoint()
    {
        float secElapsed = 0f;
        Vector3 currentPos = playerTransform.position;
        Quaternion currentRot = playerTransform.rotation;

        while (secElapsed < respawnTime)
        {
            secElapsed += Time.deltaTime;
            float t = secElapsed / respawnTime;
            playerTransform.position = Vector3.Lerp(currentPos, activeSpawnPoint.position, t);
            playerTransform.rotation = Quaternion.Lerp(currentRot, activeSpawnPoint.rotation, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        playerRB.useGravity = true;
        playerTransform.rotation = playerStartRot;
        yield return new WaitForSeconds(.2f);
        playerController.SetMovementLocked(false);
    }
}
