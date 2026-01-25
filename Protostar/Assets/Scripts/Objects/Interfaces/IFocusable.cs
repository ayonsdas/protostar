using UnityEngine;

public interface IFocusable
{
    void Focus(GameObject interactor);
    void Unfocus(GameObject interactor);
}