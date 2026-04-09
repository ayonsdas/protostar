using System;
using System.Collections;
using FMODUnity;
using UnityEngine;

public class PickupAnimator : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float liftDuration = 0.4f;
    [SerializeField] private float orbitDuration = 1.2f;
    [SerializeField] private float flyDuration = 0.4f;
    [SerializeField] private float endDuration = 0.4f;

    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 1.5f;
    [SerializeField] private float orbitHeight = 1.5f;
    [SerializeField] private float orbitCount = 1.5f;

    [Header("Scale")]
    [SerializeField] private float liftScale = 0.4f;


    [Header("Audio")]
    [SerializeField] private EventReference pickupSound;

    [Header("References")]
    [SerializeField] private Transform backpackBone;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraFollow cameraController;

    private Rigidbody playerRigidbody;
    private CustomGravityBody customGravityBody;

    public bool IsPlaying { get; private set; }

    private void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        customGravityBody = GetComponent<CustomGravityBody>();
    }

    public void PlayPickup(GameObject item, Vector3 itemForwardAxis, Action onComplete = null)
    {
        StartCoroutine(PickupSequence(item, itemForwardAxis, onComplete));
    }

    private IEnumerator PickupSequence(GameObject item, Vector3 itemForwardAxis, Action onComplete)
    {
        // Set flag to disable respawning
        IsPlaying = true;

        // Lock player
        InputModeManager.Instance.SetPlayerControlsEnabled(false);

        // Set player kinematic to get manual control of transform
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.isKinematic = true;

        // Face the item
        Vector3 directionToItem = (item.transform.position - transform.position).normalized;
        Vector3 upDirection = customGravityBody.GetUpDirection();
        directionToItem = Vector3.ProjectOnPlane(directionToItem, upDirection).normalized;

        item.transform.rotation = Quaternion.FromToRotation(itemForwardAxis.normalized, -directionToItem);
        transform.rotation = Quaternion.LookRotation(directionToItem, upDirection);


        // Play sound
        AudioManager.PlayOneShot(pickupSound, transform.position);

        // Control the camera
        Vector3 cinematicPosition = transform.position
            - directionToItem * 8f  // behind the player
            + Vector3.up * 6f;    // slightly above
        Quaternion cinematicRotation = Quaternion.LookRotation(
            item.transform.position - cinematicPosition
        );

        if (cameraController != null)
            yield return StartCoroutine(cameraController.OverrideCamera(cinematicPosition, cinematicRotation, 0.3f));

        // Disable item collision then move it
        DisableItem(item);
        yield return StartCoroutine(LiftPhase(item));
        yield return StartCoroutine(OrbitPhase(item));
        yield return StartCoroutine(FlyInPhase(item));

        // Disable item
        Destroy(item.gameObject);

        yield return new WaitForSeconds(endDuration);

        // Restore camera and player
        if (cameraController != null)
            cameraController.ReleaseCamera();

        playerRigidbody.isKinematic = false;
        InputModeManager.Instance.SetPlayerControlsEnabled(true);

        // Give extra frame to prevent respawn after disabling kinematic
        yield return new WaitForFixedUpdate();

        IsPlaying = false;
        onComplete?.Invoke();
    }

    private IEnumerator LiftPhase(GameObject item)
    {
        float t = 0f;
        Vector3 startPos = item.transform.position;
        Vector3 endPos = transform.position + Vector3.up * 4f;
        Vector3 startScale = item.transform.localScale;
        Vector3 endScale = startScale * liftScale;

        while (t < liftDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, t / liftDuration);
            item.transform.position = Vector3.Lerp(startPos, endPos, progress);
            item.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            yield return null;
        }
    }

    private IEnumerator OrbitPhase(GameObject item)
    {
        float t = 0f;
        Vector3 orbitCenter = transform.position + Vector3.up;
        float startHeight = item.transform.position.y - orbitCenter.y;

        Vector3 flatOffset = item.transform.position - orbitCenter;
        flatOffset.y = 0f;
        float startRadius = flatOffset.magnitude;

        while (t < orbitDuration)
        {
            t += Time.deltaTime;
            float progress = t / orbitDuration;

            float radius = Mathf.Lerp(startRadius, orbitRadius, Mathf.SmoothStep(0f, 1f, progress * 2f));
            radius = Mathf.Min(radius, orbitRadius);
            radius = Mathf.Lerp(radius, 0.3f, Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, progress * 2f - 1f)));
            float height = Mathf.Lerp(startHeight, orbitHeight, progress);
            float angle = progress * 360f * orbitCount * Mathf.Deg2Rad;

            item.transform.position = orbitCenter + new Vector3(
                Mathf.Cos(angle) * radius,
                height,
                Mathf.Sin(angle) * radius
            );

            yield return null;
        }
    }

    private IEnumerator FlyInPhase(GameObject item)
    {
        float t = 0f;
        Vector3 positionStart = item.transform.position;
        Vector3 scaleStart = item.transform.localScale;

        while (t < flyDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, t / flyDuration);
            item.transform.position = Vector3.Lerp(positionStart, backpackBone.position, progress);
            item.transform.localScale = Vector3.Lerp(scaleStart, Vector3.zero, progress);
            yield return null;
        }
    }

    private void DisableItem(GameObject item)
    {
        DisableFocus(item);
        DisableColliders(item);
    }

    private void DisableFocus(GameObject item)
    {
        if (item.TryGetComponent<IFocusable>(out var focusable))
        {
            focusable.Unfocus(gameObject);
        }
    }

    private void DisableColliders(GameObject item)
    {
        Collider[] colliders = item.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }
}