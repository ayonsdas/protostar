using UnityEngine;

public class PickupableObject : MonoBehaviour, IPickupable
{
    private Rigidbody rb;
    private Collider[] colliders;
    private Transform originalParent;
    private bool isPickedUp = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
    }

    public void OnPickup(GameObject picker)
    {
        if (isPickedUp) return;
        
        isPickedUp = true;
        originalParent = transform.parent;
        
        // Disable physics while carried (if it has a rigidbody)
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Disable collisions while carried (if it has colliders)
        if (colliders != null && colliders.Length > 0)
        {
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }
        
        Debug.Log($"Picked up {gameObject.name}");
    }

    public void OnDrop(GameObject picker)
    {
        if (!isPickedUp) return;
        
        isPickedUp = false;
        
        // Re-enable physics (if it has a rigidbody)
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        // Re-enable collisions (if it has colliders)
        if (colliders != null && colliders.Length > 0)
        {
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
        }
        
        Debug.Log($"Dropped {gameObject.name}");
    }
}
