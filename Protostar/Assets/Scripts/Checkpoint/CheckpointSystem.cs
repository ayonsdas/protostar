using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CheckpointSystem : MonoBehaviour
{
    [Tooltip("Index 0 should be set to the starting spawn point.")]
    public Transform[] spawnPoints;
    public PlayableLevelZone[] playableLevelZones;

    [Header("Respawn Variables")]
    public Transform playerTransform;
    private Quaternion playerStartRot;
    private Vector3 playerStartPos;
    public float respawnTime = 2f;
    public bool teleport = false;
    public bool disablePreviousZones = false;

    private bool isRespawning;
    public bool IsRespawning
    {
        get
        {
            return isRespawning;
        }
        private set
        {
            isRespawning = value;
            if (value)
                OnRespawnStart?.Invoke();
            else
                OnRespawnEnd?.Invoke();
        }
    }
    public Action OnRespawnStart;
    public Action OnRespawnEnd;

    private int activeCheckpointIndex;
    private Transform activeSpawnPoint;
    private PlayableLevelZone currentPlayableZone;
    private PlayerController playerController;
    private Rigidbody playerRB;
    private CustomGravityBody playerGravityBody;

    void Start()
    {
        DisablePlayableZones();
        SetActiveCheckpoint(0);

        playerStartPos = playerTransform.position + Vector3.up;
        playerStartRot = playerTransform.rotation;

        playerController = playerTransform.GetComponent<PlayerController>();
        playerRB = playerTransform.GetComponent<Rigidbody>();
        playerGravityBody = playerTransform.GetComponent<CustomGravityBody>();
    }

    public void SetActiveCheckpoint(int checkpointIndex)
    {
        if (checkpointIndex > activeCheckpointIndex || activeCheckpointIndex == 0)
        {
            activeCheckpointIndex = checkpointIndex;
            activeSpawnPoint = spawnPoints[activeCheckpointIndex];
            ChangeCurrentPlayableZone();
            Debug.Log("Active Checkpoint updated to Checkpoint " + activeCheckpointIndex);
        }
    }

    private void DisablePlayableZones()
    {
        foreach (PlayableLevelZone zone in playableLevelZones)
        {
            if (zone != null)
                zone.gameObject.SetActive(false);
        }
    }

    private void ChangeCurrentPlayableZone()
    {
        if (currentPlayableZone != null && disablePreviousZones)
            currentPlayableZone.gameObject.SetActive(false);

        currentPlayableZone = playableLevelZones[activeCheckpointIndex];
        if (currentPlayableZone != null)
            currentPlayableZone.gameObject.SetActive(true);
    }

    public void TryRespawnPlayer()
    {
        if (IsRespawning)
        {
            Debug.LogWarning("[CheckpointSystem] Player is already respawning. Ignoring additional respawn request.");
            return;
        }

        if (IsPlayerInside())
        {
            Debug.Log("[CheckpointSystem] Player is still inside a playable zone. No respawn needed.");
            return;
        }

        RespawnPlayer();
    }

    private bool IsPlayerInside()
    {
        foreach (PlayableLevelZone zone in playableLevelZones)
        {
            if (zone == null || !zone.isActiveAndEnabled)
                continue;
            if (zone.IsPlayerInside())
            {
                Debug.Log($"[CheckpointSystem] Player is inside zone {zone.name}");
                return true;
            }
        }
        return false;
    }

    private void RespawnPlayer()
    {
        if (IsRespawning)
        {
            Debug.LogWarning("Player is already respawning. Ignoring additional respawn request.");
            return;
        }

        IsRespawning = true;

        if (!teleport)
        {
            playerController.SetMovementLocked(true);
            playerRB.isKinematic = true;
            StartCoroutine(MovePlayerToCheckpoint());
        }
        else
        {
            playerTransform.position = activeSpawnPoint.position;
            playerTransform.rotation = activeSpawnPoint.rotation;
            playerController.SetMovementLocked(false);
            IsRespawning = false;
        }
    }

    private IEnumerator MovePlayerToCheckpoint()
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
        playerRB.isKinematic = false;
        Debug.Log("Player no longer kinematic");
        playerRB.linearVelocity = Vector3.zero;
        playerRB.angularVelocity = Vector3.zero;

        playerTransform.rotation = playerStartRot;
        // TODO spawn points should eventually specify their gravity direction, but just use down for now
        playerGravityBody.SetCustomGravityDirection(new Vector3(0, -1, 0), rotateVelocity: false);
        yield return new WaitForSeconds(.2f);
        playerController.SetMovementLocked(false);
        IsRespawning = false;
    }
}
