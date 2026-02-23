using UnityEngine.InputSystem;
using UnityEngine;
using System;
using FMODUnity;
using System.Collections.Generic;

public class PlayerInteractor : Interactor
{
    [Header("Pickup Settings")]
    [SerializeField] private Transform pickupHoldPoint; // Position above player's head
    [SerializeField] private float dropDistance = 2f; // Distance in front to drop

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 3f;
    [SerializeField] private LayerMask interactionMask;

    [Header("Interaction UI")]
    [SerializeField] private InteractableUI interactionUI; // UI element to show interaction messages

    [Header("Sound Settings")]
    [SerializeField] private bool enableShiftSound = true;
    [SerializeField] private EventReference shiftEvent;

    private GameObject _carriedObject;
    private GameObject carriedObject
    {
        set
        {
            _carriedObject = value;
            OnCarriedObjectChange.Invoke(value);
        }
        get { return _carriedObject; }
    }
    private IPickupable carriedPickupable;

    public Action<GameObject> OnCarriedObjectChange;

    // Shift state
    private bool isShiftHeld = false;
    private IEngageable activeShiftEngageable = null;
    private IShiftable activeShiftTarget = null;
    private Vector2 shiftInput = Vector2.zero; // WASD input accumulated during shift hold
    private PlayerController playerController;
    private SphereCollider triggerCollider;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        triggerCollider = GetComponent<SphereCollider>();

        if (triggerCollider == null)
            triggerCollider = gameObject.AddComponent<SphereCollider>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = interactionRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & interactionMask) == 0)
        {
            return;
        }

        var candidate = other.GetComponentInParent<IInteractionCandidate>();
        if (candidate != null && !nearby.Contains(candidate))
        {
            nearby.Add(candidate);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var candidate = other.GetComponentInParent<IInteractionCandidate>();
        if (candidate != null)
        {
            nearby.Remove(candidate);
        }
    }

    protected override PlayerInteractionContext BuildContext()
    {
        return new PlayerInteractionContext
        {
            Player = gameObject,
            CarriedObject = carriedObject,
            IsCarrying = carriedObject != null,
            SetCarriedObject = SetCarriedObjectInternal,
            DropCarriedObject = DropCarriedObjectInternal,
            ClearCarriedObject = ClearCarriedObjectInternal
        };
    }

    private void SetCarriedObjectInternal(GameObject obj)
    {
        carriedObject = obj;
        carriedPickupable = obj.GetComponent<IPickupable>();

        obj.transform.SetParent(pickupHoldPoint);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        carriedPickupable?.OnPickup(gameObject);
    }

    private void ClearCarriedObjectInternal()
    {
        carriedObject = null;
        carriedPickupable = null;
    }

    private void DropCarriedObjectInternal()
    {
        if (carriedObject == null)
            return;

        // Calculate drop position in front of player in local space
        Vector3 dropPosition = transform.position + transform.forward * dropDistance;

        // Unparent and place
        carriedObject.transform.SetParent(null);
        carriedObject.transform.position = dropPosition;

        // Notify the object
        carriedPickupable?.OnDrop(gameObject);

        ClearCarriedObjectInternal();
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed)
        {
            return;
        }

        if(!string.IsNullOrEmpty(currentOption.Prompt))
        {
            interactionUI?.Show(currentOption.Prompt);
        }

        currentOption.Execute?.Invoke();
    }

    public void OnShift(InputValue value)
    {
        if (value.isPressed)
        {
            TryBeginShift();
        }
        else
        {
            EndShift();
        }
    }

    private void TryBeginShift()
    {
        if (isShiftHeld)
            return;

        // Use the current resolved option
        if (currentOption.Type != InteractionType.Shift)
            return;

        if (currentOption.Source == null)
            return;

        activeShiftTarget = currentOption.Source.GetComponent<IShiftable>();
        activeShiftEngageable = currentOption.Source.GetComponent<IEngageable>();

        if (activeShiftTarget == null)
            return;

        isShiftHeld = true;

        activeShiftEngageable?.Engage(gameObject);
        playerController?.SetMovementLocked(true);
    }

    private void EndShift()
    {
        if (!isShiftHeld)
            return;

        int direction = CalculateShiftDirection();
        activeShiftTarget.Shift(direction);
        PlayShiftSound();

        activeShiftEngageable?.Disengage(gameObject);

        isShiftHeld = false;
        activeShiftTarget = null;
        activeShiftEngageable = null;
        shiftInput = Vector2.zero;

        playerController?.SetMovementLocked(false);
    }

    private int CalculateShiftDirection()
    {
        // Determine shift direction from accumulated WASD input
        // W/D = forward (+1), S/A = backward (-1), no input = forward (+1) default
        int direction = 1;
        if (Mathf.Abs(shiftInput.y) > Mathf.Abs(shiftInput.x))
        {
            direction = shiftInput.y >= 0 ? 1 : -1;
        }
        else if (Mathf.Abs(shiftInput.x) > 0.01f)
        {
            direction = shiftInput.x >= 0 ? 1 : -1;
        }
        return direction;
    }

    private void PlayShiftSound()
    {
        MonoBehaviour mb = activeShiftTarget as MonoBehaviour;
        if(mb != null)
            return;

        Vector3 pos = mb.gameObject.transform.position;
        // Play sound if needed
        try
        {
            AudioManager.Instance.PlayOneShot(shiftEvent, pos);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerInteractor] Error playing shift SFX: {e.Message}");
        }
    }

    public void OnMove(InputValue value)
    {
        // When shift is held, capture WASD as shift input instead of movement
        if (isShiftHeld)
        {
            shiftInput = value.Get<Vector2>();
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}