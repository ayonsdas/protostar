using UnityEngine;
using FMODUnity;

/// <summary>
/// A specific placement location for a seed around the sapling puzzle.
/// Put a collider (trigger or non-trigger) on this object on the Interactable layer.
/// The player can place a carried seed here with F, or remove it with F.
/// </summary>
public class SeedSlot : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] private GameObject emptyVisual;  // Indicator when slot is empty (e.g. glowing circle)
    [SerializeField] private GameObject filledVisual; // Indicator when slot is filled

    [Header("Placement")]
    [Tooltip("Local offset for where the seed sits when placed. Use negative Y to place it lower/on the ground.")]
    [SerializeField] private Vector3 seedPlacementOffset = new Vector3(0f, -0.05f, 0f);

    [Header("Sound")]
    [field: SerializeField] public EventReference seedPlaceSoundEvent { get; private set; }

    public SeedObject PlacedSeed { get; private set; }
    public bool IsFilled => PlacedSeed != null;

    /// <summary>
    /// Fired when a seed is placed or removed. SaplingPuzzle subscribes to this.
    /// </summary>
    public System.Action OnSlotChanged;

    private void Start()
    {
        UpdateVisuals();
    }

    /// <summary>
    /// IInteractable — not called directly during normal flow;
    /// PlayerInteractor intercepts seed-slot interactions before reaching here.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        Debug.Log($"[SeedSlot] Interact called on {gameObject.name}, IsFilled={IsFilled}");
    }

    /// <summary>
    /// Place a seed in this slot. Returns true if successful.
    /// The seed should already have been unparented / dropped before calling this.
    /// </summary>
    public bool PlaceSeed(SeedObject seed)
    {
        if (IsFilled || seed == null)
        {
            Debug.Log($"[SeedSlot] PlaceSeed rejected: IsFilled={IsFilled}, seed={(seed != null ? seed.name : "null")}");
            return false;
        }

        PlacedSeed = seed;

        // Snap seed to slot position (with offset so it sits on the ground)
        seed.transform.SetParent(transform);
        seed.transform.localPosition = seedPlacementOffset;
        seed.transform.localRotation = Quaternion.identity;

        // Put seed into slot state
        seed.OnPlaceInSlot();

        // Update visuals FIRST (before sound)
        Debug.Log($"[SeedSlot] Seed '{seed.name}' placed in slot '{gameObject.name}'");
        UpdateVisuals();
        OnSlotChanged?.Invoke();

        // Play sound (completely independent — failure does not affect anything)
        try
        {
            RuntimeManager.PlayOneShot(seedPlaceSoundEvent, transform.position);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SeedSlot] Failed to play place sound: {e.Message}");
        }

        return true;
    }

    /// <summary>
    /// Remove the seed from this slot. Returns the removed seed, or null.
    /// </summary>
    public SeedObject RemoveSeed()
    {
        if (!IsFilled) return null;

        SeedObject seed = PlacedSeed;
        PlacedSeed = null;

        seed.transform.SetParent(null);
        seed.OnRemoveFromSlot();

        UpdateVisuals();
        OnSlotChanged?.Invoke();

        Debug.Log($"[SeedSlot] Seed '{seed.name}' removed from slot '{gameObject.name}'");
        return seed;
    }

    /// <summary>
    /// Called by SaplingPuzzle when the puzzle completes — consume the seed permanently.
    /// </summary>
    public void ConsumeSeed()
    {
        if (PlacedSeed != null)
        {
            PlacedSeed.gameObject.SetActive(false);
            PlacedSeed = null;
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        bool filled = IsFilled;
        Debug.Log($"[SeedSlot] UpdateVisuals on '{gameObject.name}': filled={filled}, emptyVisual={(emptyVisual != null ? emptyVisual.name : "null")}, filledVisual={(filledVisual != null ? filledVisual.name : "null")}");

        if (emptyVisual != null)
        {
            emptyVisual.SetActive(!filled);
        }

        if (filledVisual != null)
        {
            filledVisual.SetActive(filled);
        }
    }
}
