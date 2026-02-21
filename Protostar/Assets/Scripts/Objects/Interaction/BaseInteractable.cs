using UnityEngine;
using System;

public abstract class BaseInteractable : MonoBehaviour, IInteractable
{
    public event Action OnInteracted;

    public InteractionResult Interact(GameObject interactor)
    {
        if (!CanInteract(out string message))
        {
            OnInteractFailure(interactor);
            return InteractionResult.Failure(message);
        }

        OnInteractSuccess(interactor);
        OnInteracted?.Invoke();
        return InteractionResult.SuccessResult();
    }

    protected abstract void OnInteractSuccess(GameObject interactor);
    protected virtual void OnInteractFailure(GameObject interactor) { }
    protected virtual bool CanInteract(out string message)
    {
        message = string.Empty;
        return true;
    }
}