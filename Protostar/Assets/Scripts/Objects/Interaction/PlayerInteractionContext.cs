using UnityEngine;

/// <summary>
/// Available player context passed in IInteractionCandidate.CollectOptions to specify available options
/// </summary>
public class PlayerInteractionContext
{
    public GameObject Player;
    public GameObject CarriedObject;
    public bool IsCarrying;
}