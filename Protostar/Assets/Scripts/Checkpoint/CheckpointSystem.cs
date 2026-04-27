using System;
using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public Transform respawnVfxTransform;
    public RespawnOrbVFX respawnOrbVFX;
    private Image fadeImage;
    private Quaternion playerStartRot;
    private Vector3 playerStartPos;
    public float respawnTime = 2f;
    public float teleportFadeTime = 0.8f;
    public bool teleport = false;
    public bool disablePreviousZones = false;
    public bool respawnAtLastPlatform = false;

    [Header("Sound Settings")]
    [SerializeField] private EventReference respawnSound;

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
    private PickupAnimator pickupAnimator;

    void OnEnable()
    {
        if (InputModeManager.Instance != null)
        {
            InputModeManager.PlayerInput.actions["Respawn"].performed += HandleRespawnAction;
        }
    }

    void OnDisable()
    {
        if (InputModeManager.Instance != null)
        {
            InputModeManager.PlayerInput.actions["Respawn"].performed -= HandleRespawnAction;
        }
    }

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
        pickupAnimator = playerTransform.GetComponent<PickupAnimator>();
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
            try
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetMusicParameter("Orchestration", activeCheckpointIndex);
                    Debug.Log($"[CheckpointSystem] Set Music Orchestration parameter to {activeCheckpointIndex}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CheckpointSystem] Failed to set music orchestration parameter: {e.Message}");
            }
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

    private void HandleRespawnAction(InputAction.CallbackContext _)
    {
        if (!CanStartRespawn(ignoreZones: true)) return;
        Debug.Log("Starting Player Respawn");

        RespawnPlayer(false);
    }

    public void TryRespawnPlayer()
    {
        if (!CanStartRespawn()) return;

        RespawnPlayer(respawnAtLastPlatform);
    }

    private bool CanStartRespawn(bool ignoreZones = false)
    {
        if (IsRespawning)
        {
            Debug.LogWarning("[CheckpointSystem] Player is already respawning. Ignoring additional respawn request.");
            return false;
        }

        if (!ignoreZones && IsPlayerInside())
        {
            Debug.Log("[CheckpointSystem] Player is still inside a playable zone. No respawn needed.");
            return false;
        }

        // In the middle of item pickup animation, don't trigger respawn
        if (pickupAnimator != null && pickupAnimator.IsPlaying)
        {
            return false;
        }

        return true;
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

    private void RespawnPlayer(bool onPlatform)
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

        if (onPlatform)
        {
            spawnPointData = new SpawnPoint
            {
                Position = playerController.LastGroundedState.Position,
                Rotation = playerController.LastGroundedState.Rotation,
                GravityDirection = playerController.LastGroundedState.GravityDirection
            };
            Debug.Log($"[CheckpointSystem] Respawning at last grounded platform Position: {spawnPointData.Position}, Rotation: {spawnPointData.Rotation}, GravityDirection: {spawnPointData.GravityDirection}");
        }

        try
        {
            AudioManager.PlayOneShot(respawnSound);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CheckpointSystem] Failed to play respawn sound: {e.Message}");
        }

        if (!teleport)
        {
            playerController.SetMovementLocked(true);
            playerRB.isKinematic = true;
            StartCoroutine(MovePlayerToCheckpoint(spawnPointData));
        }
        else
        {
            playerController.SetMovementLocked(true);
            playerRB.isKinematic = true;
            StartCoroutine(TeleportToCheckpoint(spawnPointData));
        }
    }

    private IEnumerator TeleportToCheckpoint(SpawnPoint spawnPointData)
    {
        Color transparentColor = new Color(0, 0, 0, 0);
        Color opaqueColor = new Color(0, 0, 0, 1);
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
            cameraFollow.ResetCameraOffset();

        respawnOrbVFX.SpawnOrbs(); // spawn while screen is still black

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

        respawnOrbVFX.AbsorbOrbs(); // absorb, then unlock movement after absorption finishes
        yield return new WaitForSeconds(respawnOrbVFX.absorptionDuration + 0.01f);

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
