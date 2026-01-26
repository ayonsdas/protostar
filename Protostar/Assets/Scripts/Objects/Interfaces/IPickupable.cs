using UnityEngine;

public interface IPickupable
{
    void OnPickup(GameObject picker);
    void OnDrop(GameObject picker);
}
