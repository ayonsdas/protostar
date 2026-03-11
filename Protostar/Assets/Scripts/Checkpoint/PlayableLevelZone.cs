using UnityEngine;

public class PlayableLevelZone : MonoBehaviour
{
    PlayableZone[] playableZones;

    private void Start()
    {
        playableZones = GetComponentsInChildren<PlayableZone>();
    }

    public bool IsPlayerInside()
    {
        foreach (PlayableZone zone in playableZones)
        {
            if (zone.PlayerInside)
                return true;
        }
        return false;
    }
}
