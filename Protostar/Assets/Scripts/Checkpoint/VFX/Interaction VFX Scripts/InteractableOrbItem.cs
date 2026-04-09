using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


/// Handles player interaction with an orb collectible.
/// When the player is within range and presses F, all orbiting orbs
/// are absorbed toward the player and destroyed.
[RequireComponent(typeof(OrbVFX))]
public class InteractableOrbItem : MonoBehaviour
{
    [Header("Absorption Settings")]
    [Tooltip("Time in seconds for each orb to travel to the player and shrink away.")]
    public float absorptionDuration = 0.8f;

    private OrbVFX orbSystem;

    void Start()
    {
        orbSystem = GetComponent<OrbVFX>();
    }

    public IEnumerator AbsorbOrbs(Transform playerTransform)
    {
        GameObject[] orbs = orbSystem.GetOrbs();

        // Attach an absorb behaviour to every active orb.
        foreach (var orb in orbs)
        {
            if (orb != null)
            {
                var absorb = orb.AddComponent<OrbAbsorb>();
                absorb.AbsorbToTarget(playerTransform, absorptionDuration);
            }
        }

        // Wait for all orbs to finish absorbing
        yield return new WaitForSeconds(absorptionDuration + 0.2f);
    }
}