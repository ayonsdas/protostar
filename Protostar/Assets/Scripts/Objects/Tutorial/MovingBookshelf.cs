using UnityEngine;

/// <summary>
/// Moving bookshelf object in the tutorial.
/// Waits until book has been placed, then activates animation clip
/// Can be shifted (Left Shift) to grow into a tree the player can jump on.
/// </summary>
public class MovingBookshelf : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string trigger;

    [Header("Puzzle")]
    [SerializeField] private BookSlot slot;

    private bool triggered = false;

    private void OnEnable()
    {
        if (slot != null)
        {
            slot.OnSlotChanged += OnSlotChanged;
        }
    }

    private void OnDisable()
    {
        if (slot != null)
        {
            slot.OnSlotChanged -= OnSlotChanged;
        }
    }

    private void OnSlotChanged()
    {
        if (animator == null) return;

        if (!triggered && CompletedPuzzle())
        {
            triggered = true;
            slot.Lock();
            animator.SetTrigger(trigger);
            
        }
    }

    private bool CompletedPuzzle()
    {
        return slot != null && slot.IsFilled;
    }
}
