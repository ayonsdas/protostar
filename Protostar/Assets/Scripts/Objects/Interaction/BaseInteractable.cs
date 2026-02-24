using UnityEngine;
using System;

public abstract class BaseInteractable : MonoBehaviour, IInteractable
{
    public event Action OnInteracted;

    public void Interact(GameObject interactor)
    {
        OnInteract(interactor);
    }

    protected abstract void OnInteract(GameObject interactor);
}