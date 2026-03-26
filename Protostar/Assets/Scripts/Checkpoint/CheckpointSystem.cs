using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CheckpointSystem : MonoBehaviour
{
    private struct SpawnPoint
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 GravityDirection;
    }

    [Tooltip("Index 0 should be set to the starting spawn point.")]
    public Transform[] spawnPoints;
    public PlayableLevelZone[] playableLevelZones;

    [Header("Respawn Variables")]
    public Transform playerTransform;
    public Canvas fadeEffectCanvas;
    private Image fadeImage;
    private Quaternion playerStartRot;
    private Vector3 playerStartPos;
    public float respawnTime = 2f;
    public float teleportFadeTime = 0.8f;
    public bool teleport = false;
    public bool disablePreviousZones = false;
    public bool respawnAtLastPlatform = false;

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
        if (fadeEffectCanvas != null)
        {
            fadeImage = fadeEffectCanvas.GetComponentInChildren<Image>();
            fadeEffectCanvas.gameObject.SetActive(false);
        }
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

            // Update the Orchestration parameter in AudioManager
            AudioManager.Instance.SetMusicParameter("Orchestration", activeCheckpointIndex);
            Debug.Log($"[CheckpointSystem] Set Music Orchestration parameter to {activeCheckpointIndex}");
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

        SpawnPoint spawnPointData = new SpawnPoint
        {
            Position = activeSpawnPoint.position,
            Rotation = activeSpawnPoint.rotation,
            GravityDirection = -activeSpawnPoint.up
        };

        if (respawnAtLastPlatform)
        {
            spawnPointData = new SpawnPoint
            {
                Position = playerController.LastGroundedState.Position,
                Rotation = playerController.LastGroundedState.Rotation,
                GravityDirection = playerController.LastGroundedState.GravityDirection
            };
            Debug.Log($"[CheckpointSystem] Respawning at last grounded platform Position: {spawnPointData.Position}, Rotation: {spawnPointData.Rotation}, GravityDirection: {spawnPointData.GravityDirection}");
        }

        if (!teleport)
        {
            playerController.SetMovementLocked(true);
            playerRB.isKinematic = true;
            StartCoroutine(MovePlayerToCheckpoint(spawnPointData));
        }
        else
        {
            // playerTransform.position = spawnPointData.Position;
            // playerTransform.rotation = spawnPointData.Rotation;
            // playerGravityBody.SetCustomGravityDirection(spawnPointData.GravityDirection, rotateVelocity: false);
            // playerController.SetMovementLocked(false);
            // IsRespawning = false;

            playerController.SetMovementLocked(true);
            playerRB.isKinematic = true;
            StartCoroutine(TeleportToCheckpoint(spawnPointData));
        }
    }

    private IEnumerator TeleportToCheckpoint(SpawnPoint spawnPointData)
    {
        Color transparentColor = new Color(0, 0, 0, 0); // empty
        Color opaqueColor = new Color(0, 0, 0, 1); // black

        fadeImage.color = transparentColor;
        fadeEffectCanvas.gameObject.SetActive(true);

        float secElapsed = 0f;
        while (secElapsed < teleportFadeTime) // fade to black
        {
            secElapsed += Time.deltaTime;
            float t = secElapsed / teleportFadeTime;
            fadeImage.color = Color.Lerp(transparentColor, opaqueColor, t);
            yield return null;
        }

        playerTransform.position = spawnPointData.Position;
        playerTransform.rotation = spawnPointData.Rotation;
        playerGravityBody.SetCustomGravityDirection(spawnPointData.GravityDirection, rotateVelocity: false);
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.ResetCameraOffset();
        }
        Debug.Log($"[CheckpointSystem] Teleported to Position: {spawnPointData.Position}, Rotation: {spawnPointData.Rotation}, GravityDirection: {spawnPointData.GravityDirection}");
        fadeImage.color = opaqueColor;

        yield return new WaitForSeconds(.2f);

        secElapsed = 0f;
        while (secElapsed < teleportFadeTime) // fade to transparent
        {
            secElapsed += Time.deltaTime;
            float t = secElapsed / teleportFadeTime;
            fadeImage.color = Color.Lerp(opaqueColor, transparentColor, t);
            yield return null;
        }

        playerRB.isKinematic = false;
        playerRB.linearVelocity = Vector3.zero;
        playerRB.angularVelocity = Vector3.zero;
        fadeEffectCanvas.gameObject.SetActive(false);

        yield return new WaitForSeconds(.1f);
        playerController.SetMovementLocked(false);
        IsRespawning = false;
    }

    private IEnumerator MovePlayerToCheckpoint(SpawnPoint spawnPointData)
    {
        float secElapsed = 0f;
        Vector3 currentPos = playerTransform.position;
        Quaternion currentRot = playerTransform.rotation;

        while (secElapsed < respawnTime)
        {
            secElapsed += Time.deltaTime;
            float t = secElapsed / respawnTime;
            playerTransform.position = Vector3.Lerp(currentPos, spawnPointData.Position, t);
            playerTransform.rotation = Quaternion.Lerp(currentRot, spawnPointData.Rotation, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        playerRB.isKinematic = false;
        Debug.Log("Player no longer kinematic");
        playerRB.linearVelocity = Vector3.zero;
        playerRB.angularVelocity = Vector3.zero;

        playerTransform.rotation = playerStartRot;
        playerGravityBody.SetCustomGravityDirection(spawnPointData.GravityDirection, rotateVelocity: false);
        yield return new WaitForSeconds(.2f);
        playerController.SetMovementLocked(false);
        IsRespawning = false;
    }
}
