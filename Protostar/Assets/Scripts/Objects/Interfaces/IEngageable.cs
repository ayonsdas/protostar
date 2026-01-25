using UnityEngine;

public interface IEngageable
{
    void Engage(GameObject interactor);
    void Disengage(GameObject interactor);
}