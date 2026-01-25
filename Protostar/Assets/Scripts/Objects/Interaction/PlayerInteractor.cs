using UnityEngine.InputSystem;
using UnityEngine;

public class PlayerInteractor : Interactor
{
    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("Interacted");
            Interact();
        }
    }

    public void OnShift(InputValue value)
    {
        int direction = value.Get<float>() > 0 ? 1 : -1;
        Scroll(direction);
        Debug.Log("Scrolled in " + direction);
    }
}