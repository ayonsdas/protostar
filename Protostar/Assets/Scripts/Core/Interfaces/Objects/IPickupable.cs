using UnityEngine;

public interface IPickupable
{
    bool CanPickup();
    void OnPickup(GameObject picker);
    void OnDrop(GameObject picker);
}
