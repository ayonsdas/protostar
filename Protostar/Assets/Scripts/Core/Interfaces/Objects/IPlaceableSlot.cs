using UnityEngine;

public interface IPlaceableSlot
{
    bool IsFilled { get; }
    bool TryPlace(GameObject obj);
    GameObject TryRemove();
}