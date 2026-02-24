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

    // BUTTON CALLBACKS
    public void OnShift(InputValue value)
    {
        InteractionOption bestOption;
        // No option for shift exists
        if(!_bestOptionByInput.TryGetValue(InteractionInputType.Shift, out bestOption))
        {
            Debug.Log($"[PlayerInteractor] No available shift option, Shift pressed: {value.isPressed} Best option: {bestOption}");
            return;
        }

        if(!string.IsNullOrEmpty(bestOption.Prompt))
        {
            interactionUI?.Show(bestOption.Prompt);
        }

        if (value.isPressed)
        {
            Debug.Log($"[PlayerInteractor] Invoking shift press, Best option: {bestOption} Source: {bestOption.Source.name}");
            bestOption.OnPressed?.Invoke(this);
        }
        else
        {
            Debug.Log($"[PlayerInteractor] Invoking shift release, Best option: {bestOption} Source: {bestOption.Source.name}");
            bestOption.OnReleased?.Invoke(this);
        }
    }

    public void OnInteract(InputValue value)
    {
        InteractionOption bestOption;
        // No option for interaction exists
        if(!_bestOptionByInput.TryGetValue(InteractionInputType.Interact, out bestOption))
        {
            return;
        }

        if(!string.IsNullOrEmpty(bestOption.Prompt))
        {
            interactionUI?.Show(bestOption.Prompt);
        }

        if(value.isPressed)
        {
            bestOption.OnPressed?.Invoke(this);
        }
        else
        {
            bestOption.OnReleased?.Invoke(this);
        }

    }

    // FUNCTIONS TO BE USED IN INTERACTION OPTIONS
    protected override PlayerInteractionContext BuildContext()
    {
        return new PlayerInteractionContext
        {
            Player = gameObject,
            CarriedObject = carriedObject,
            IsCarrying = carriedObject != null
        };
    }

    public void PickupObject(MonoBehaviour source)
    {
        IPickupable pickupable = source.GetComponent<IPickupable>();
        if(pickupable == null)
            return;

        carriedPickupable = pickupable;
        carriedObject = source.gameObject;

        carriedObject.transform.SetParent(pickupHoldPoint);
        carriedObject.transform.localPosition = Vector3.zero;
        carriedObject.transform.localRotation = Quaternion.identity;

        carriedPickupable?.OnPickup(gameObject);
    }

    public void PickupObject(GameObject obj)
    {
        IPickupable pickupable = obj.GetComponent<IPickupable>();
        if(pickupable == null)
            return;

        carriedPickupable = pickupable;
        carriedObject = obj;

        carriedObject.transform.SetParent(pickupHoldPoint);
        carriedObject.transform.localPosition = Vector3.zero;
        carriedObject.transform.localRotation = Quaternion.identity;

        carriedPickupable?.OnPickup(gameObject);
    }

    public void ClearCarriedObject()
    {
        carriedObject = null;
        carriedPickupable = null;
    }

    public void DropCarriedObject()
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

        ClearCarriedObject();
    }

    public void TryPlaceInto(MonoBehaviour source)
    {
        if (carriedObject == null)
            return;

        var slot = source.GetComponent<IPlaceableSlot>();
        if (slot == null)
            return;

        if(slot.TryPlace(carriedObject))
        {
            ClearCarriedObject();
        }
    }

    public void TryTakeFrom(MonoBehaviour source)
    {
        var slot = source.GetComponent<IPlaceableSlot>();
        if (slot == null)
            return;

        var obj = slot.TryRemove();

        if (obj == null)
            return;

        PickupObject(obj);
    }

    public void InteractWithObject(MonoBehaviour source)
    {
        var interactable = source.GetComponent<IInteractable>();
        if (interactable == null)
            return;

        interactable.Interact(gameObject);
    }

    public void TryBeginShift(MonoBehaviour source)
    {

        // Already started shifting, don't need to do again
        if (isShiftHeld)
            return;

        activeShiftTarget = source.GetComponent<IShiftable>();
        activeShiftEngageable = source.GetComponent<IEngageable>();

        if (activeShiftTarget == null)
            return;

        isShiftHeld = true;

        activeShiftEngageable?.Engage(gameObject);
        playerController?.SetMovementLocked(true);
    }

    public void EndShift()
    {
        Debug.Log($"[PlayerInteractor] ending shift with {activeShiftTarget} shift held: {isShiftHeld}");
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