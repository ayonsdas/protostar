using UnityEngine;


public struct InteractionResult
{
    public bool Success;
    public string Message;
    public static InteractionResult SuccessResult(string message = null)
    {
        return new InteractionResult
        {
            Success = true,
            Message = message ?? string.Empty
        };
    }

    public static InteractionResult Failure(string message = null)
    {
        return new InteractionResult
        {
            Success = false,
            Message = message ?? string.Empty
        };
    }
}

public interface IInteractable
{
    InteractionResult Interact(GameObject interactor);
}