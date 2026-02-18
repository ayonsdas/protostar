using FMODUnity;
using UnityEngine;

public class Slot<T> : MonoBehaviour, IPlaceableSlot, IInteractable where T : MonoBehaviour, IPlaceable
{

    [Header("Visuals")]
    [SerializeField] private GameObject emptyVisual;  // Indicator when slot is empty (e.g. glowing circle)
    [SerializeField] private GameObject filledVisual; // Indicator when slot is filled

    [Header("Placement")]
    [Tooltip("Local offset for where the object sits when placed. Use negative Y to place it lower/on the ground.")]
    [SerializeField] private Vector3 objectPlacementOffset = new Vector3(0f, -0.05f, 0f);

    [Header("Sound")]
    [field: SerializeField] public EventReference objectPlaceSoundEvent { get; private set; }

    public System.Action OnSlotChanged;
    public bool IsFilled => _placedObject != null;

    private T _placedObject;
    private bool _isLocked = false;

    private void Start()
    {
        UpdateVisuals();
    }

    public void Lock(bool val = true)
    {
        _isLocked = val;
    }

    /// <summary>
    /// IInteractable — not called directly during normal flow;
    /// PlayerInteractor intercepts object-slot interactions before reaching here.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        Debug.Log($"[Slot] Interact called on {gameObject.name}, IsFilled={IsFilled}");
    }

    /// <summary>
    /// Place a object in this slot. Returns true if successful.
    /// The object should already have been unparented / dropped before calling this.
    /// </summary>
    public bool PlaceObject(T obj)
    {
        if (IsFilled || obj == null)
        {
            Debug.Log($"[Slot] PlaceObject rejected: IsFilled={IsFilled}, placedObject={(_placedObject != null ? _placedObject.name : "null")}");
            return false;
        }

        _placedObject = obj;

        // Snap object to slot position (with offset so it sits on the ground)
        obj.transform.SetParent(transform);
        obj.transform.localPosition = objectPlacementOffset;
        obj.transform.localRotation = Quaternion.identity;

        // Put object into slot state
        obj.OnPlace();

        // Update visuals FIRST (before sound)
        Debug.Log($"[Slot] object '{obj.name}' placed in slot '{gameObject.name}'");
        UpdateVisuals();
        OnSlotChanged?.Invoke();

        // Play sound (completely independent — failure does not affect anything)
        try
        {
            RuntimeManager.PlayOneShot(objectPlaceSoundEvent, transform.position);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Slot] Failed to play place sound: {e.Message}");
        }

        return true;
    }

    /// <summary>
    /// Remove the object from this slot. Returns the removed object, or null.
    /// </summary>
    public T RemoveObject()
    {
        if (!IsFilled) return null;

        // If locked, we can no longer remove the object
        if (_isLocked) return null;

        T obj = _placedObject;
        _placedObject = null;

        obj.transform.SetParent(null);
        obj.OnRemove();

        UpdateVisuals();
        OnSlotChanged?.Invoke();

        Debug.Log($"[Slot] Object '{obj.name}' removed from slot '{gameObject.name}'");
        return obj;
    }

    /// <summary>
    /// Called by SaplingPuzzle when the puzzle completes — consume the object permanently.
    /// </summary>
    public void ConsumeObject()
    {
        if (_placedObject= null)
        {
            _placedObject.gameObject.SetActive(false);
            _placedObject = null;
            UpdateVisuals();
        }
    }
    private void UpdateVisuals()
    {
        bool filled = IsFilled;
        Debug.Log($"[Slot] UpdateVisuals on '{gameObject.name}': filled={filled}, emptyVisual={(emptyVisual != null ? emptyVisual.name : "null")}, filledVisual={(filledVisual != null ? filledVisual.name : "null")}");

        if (emptyVisual != null)
        {
            emptyVisual.SetActive(!filled);
        }

        if (filledVisual != null)
        {
            filledVisual.SetActive(filled);
        }
    }

    public bool TryPlace(GameObject obj)
    {
        T component = obj.GetComponentInChildren<T>();
        if (component == null) return false;

        return PlaceObject(component);
    }

    public GameObject TryRemove()
    {
        return RemoveObject()?.gameObject;
    }
}
