using UnityEngine.InputSystem;
using UnityEngine;
using System;
using FMODUnity;

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
    private IShiftable activeShiftTarget = null;
    private Vector2 shiftInput = Vector2.zero; // WASD input accumulated during shift hold
    private PlayerController playerController;
    // Sphere collider for Interaction detection
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

    // Used with the sphere collider in order to detect interactables nearby
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
    /// <summary>
    /// Callback hooking the PlayerInput Shift action to use best InteractionOption
    /// of InteractionInputType = Shift
    /// </summary>
    public void OnShift(InputValue value)
    {
        InteractionOption bestOption;
        // No option for shift exists
        if (!_bestOptionByInput.TryGetValue(InteractionInputType.Shift, out bestOption))
        {
            // Debug.Log($"[PlayerInteractor] No available shift option, Shift pressed: {value.isPressed} Best option: {bestOption}");
            return;
        }

        // Display overlay prompt if present
        if (!string.IsNullOrEmpty(bestOption.Prompt))
        {
            interactionUI?.Show(bestOption.Prompt);
        }

        if (value.isPressed)
        {
            // Debug.Log($"[PlayerInteractor] Invoking shift press, Best option: {bestOption} Source: {bestOption.Source.name}");
            bestOption.OnPressed?.Invoke(this);
        }
        else
        {
            // Debug.Log($"[PlayerInteractor] Invoking shift release, Best option: {bestOption} Source: {bestOption.Source.name}");
            bestOption.OnReleased?.Invoke(this);
        }
    }

    /// <summary>
    /// Callback hooking the PlayerInput Interact action to use best InteractionOption
    /// of InteractionInputType = Interact
    /// </summary>
    public void OnInteract(InputValue value)
    {
        InteractionOption bestOption;
        // No option for interaction exists
        if (!_bestOptionByInput.TryGetValue(InteractionInputType.Interact, out bestOption))
        {
            return;
        }

        if (!string.IsNullOrEmpty(bestOption.Prompt))
        {
            interactionUI?.Show(bestOption.Prompt);
        }

        if (value.isPressed)
        {
            bestOption.OnPressed?.Invoke(this);
        }
        else
        {
            bestOption.OnReleased?.Invoke(this);
        }
    }

    /// <summary>
    /// Callback hooking the PlayerInput Move action to track input during shifting
    /// </summary>
    public void OnMove(InputValue value)
    {
        // When shift is held, capture WASD as shift input instead of movement
        if (isShiftHeld)
        {
            shiftInput = value.Get<Vector2>();
        }
    }

    // FUNCTIONS TO BE USED IN INTERACTION OPTIONS
    /// <summary>
    /// Builds player state context information for interactables to evaluate
    /// and then choose available options
    /// </summary>
    protected override PlayerInteractionContext BuildContext()
    {
        return new PlayerInteractionContext
        {
            Player = gameObject,
            CarriedObject = carriedObject,
            IsCarrying = carriedObject != null
        };
    }

    /// <summary>
    /// Handles logic of picking up an object and having it follow the player
    /// </summary>
    public void PickupObject(MonoBehaviour source)
    {
        // Ensure that there is a Pickupable component
        IPickupable pickupable = source.GetComponent<IPickupable>();
        if (pickupable == null)
            return;

        carriedPickupable = pickupable;
        carriedObject = source.gameObject;

        // Make the carried object follow the pickup point
        carriedObject.transform.SetParent(pickupHoldPoint);
        carriedObject.transform.localPosition = Vector3.zero;
        carriedObject.transform.localRotation = Quaternion.identity;

        carriedPickupable?.OnPickup(gameObject);
    }

    /// <summary>
    /// Handles logic of picking up an object and having it follow the player
    /// </summary>
    public void PickupObject(GameObject obj)
    {
        // Ensure that there is a Pickupable component
        IPickupable pickupable = obj.GetComponent<IPickupable>();
        if (pickupable == null)
            return;

        carriedPickupable = pickupable;
        carriedObject = obj;

        // Make the carried object follow the pickup point
        carriedObject.transform.SetParent(pickupHoldPoint);
        carriedObject.transform.localPosition = Vector3.zero;
        carriedObject.transform.localRotation = Quaternion.identity;

        // Notify the object
        carriedPickupable?.OnPickup(gameObject);
    }

    /// <summary>
    /// Allows InteractionOptions to clear object, used for placing objects in slots
    /// </summary>
    public void ClearCarriedObject()
    {
        carriedObject = null;
        carriedPickupable = null;
    }

    /// <summary>
    /// Handles logic of dropping up an object onto the floor
    /// </summary>
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

    /// <summary>
    /// Handles logic of slot placement
    /// </summary>
    public void TrySlotPlace(MonoBehaviour source)
    {
        if (carriedObject == null)
            return;

        // Check if source contains an IPlaceableSlot, if so we can try to place
        var slot = source.GetComponent<IPlaceableSlot>();
        if (slot == null)
            return;

        // If we are able to place our object, then ensure it is no longer held
        if (slot.TryPlace(carriedObject))
        {
            ClearCarriedObject();
        }
    }

    /// <summary>
    /// Handles logic of removing an object from a slot
    /// </summary>
    public void TrySlotRemove(MonoBehaviour source)
    {
        // Check if source contains an IPlaceableSlot, if so we can try to remove
        var slot = source.GetComponent<IPlaceableSlot>();
        if (slot == null)
            return;

        // If we are able to remove an object, then pick it up
        var obj = slot.TryRemove();

        if (obj == null)
            return;

        PickupObject(obj);
    }

    /// <summary>
    /// Handles logic of interacting with an IInteractable object
    /// </summary>
    public void InteractWithObject(MonoBehaviour source)
    {
        var interactable = source.GetComponent<IInteractable>();
        if (interactable == null)
            return;

        interactable.Interact(gameObject);
    }

    /// <summary>
    /// Handles logic of beginning the shift process on an object
    /// </summary>
    public void TryBeginShift(MonoBehaviour source)
    {
        // Already started shifting, don't need to do again
        if (isShiftHeld)
            return;

        activeShiftTarget = source.GetComponent<IShiftable>();
        activeEngageable = source.GetComponent<IEngageable>();

        if (activeShiftTarget == null || activeEngageable == null)
            return;

        isShiftHeld = true;

        activeEngageable.Engage(gameObject);
        playerController?.SetMovementLocked(true);
    }

    /// <summary>
    /// Handles logic of ending the shift process on an object
    /// </summary>
    public void EndShift()
    {
        if (!isShiftHeld)
            return;

        Debug.Log("[PlayerInteractor] ending shift");
        int direction = CalculateShiftDirection();
        activeShiftTarget.Shift(direction);
        PlayShiftSound();

        activeEngageable?.Disengage(gameObject);

        isShiftHeld = false;
        activeShiftTarget = null;
        activeEngageable = null;
        shiftInput = Vector2.zero;

        playerController?.SetMovementLocked(false);
    }

    public void ToggleEngage(MonoBehaviour source, bool lockMovement = true)
    {
        if (!IsEngaged)
        {
            StartEngage(source, lockMovement);
        }
        else
        {
            EndEngage();
        }
    }

    /// <summary>
    /// Handles logic of starting engagement with an object
    /// </summary>
    public void StartEngage(MonoBehaviour source, bool lockMovement = true)
    {
        if (IsEngaged)
            return;

        activeEngageable = source.GetComponent<IEngageable>();
        engagedCandidate = source.GetComponent<IInteractionCandidate>();

        if (activeEngageable == null || engagedCandidate == null)
        {
            activeEngageable = null;
            engagedCandidate = null;
            return;
        }

        activeEngageable.Engage(gameObject);
        playerController?.SetMovementLocked(lockMovement);
    }

    /// <summary>
    /// Handles logic of ending engagement with an object
    /// </summary>
    public void EndEngage()
    {
        if (!IsEngaged)
            return;

        activeEngageable?.Disengage(gameObject);
        activeEngageable = null;

        playerController?.SetMovementLocked(false);
    }

    /// <summary>
    /// Gives the direction to shift the object state
    /// </summary>
    /// <returns>The direction based on accumulated WASD input during shift</returns>
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
        Debug.Log($"[PlayerInteractor] playing shift sound target type: {activeShiftTarget?.GetType()}");
        MonoBehaviour mb = activeShiftTarget as MonoBehaviour;
        if (mb == null)
        {
            Debug.LogWarning($"[PlayerInteractor] Cannot play shift sound {activeShiftTarget} cannot be cast to MonoBehaviour");
            return;
        }

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}