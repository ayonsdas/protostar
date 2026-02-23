using System;
using UnityEngine;

public class PlayerInteractionContext
{
    public GameObject Player;
    public GameObject CarriedObject;
    public bool IsCarrying;
    public bool IsShiftPressed;

    public Action<GameObject> SetCarriedObject;
    public Action DropCarriedObject;
    public Action ClearCarriedObject;
}