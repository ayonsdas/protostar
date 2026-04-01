using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


/// Handles player interaction with an orb collectible.
/// When the player is within range and presses F, all orbiting orbs
/// are absorbed toward the player and destroyed.
public class InteractableOrbItem : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Maximum distance the player can be from this item to interact (press F).")]
    public float interactRange = 3f;

    [Header("Absorption Settings")]
    [Tooltip("Time in seconds for each orb to travel to the player and shrink away.")]
    public float absorptionDuration = 0.8f;


    private OrbVFX orbSystem;
    private Transform player;
    private bool absorbed = false;

    void Start()
    {
        orbSystem = GetComponent<OrbVFX>();

        // Find the player by tag
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (absorbed || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Interact when player is close enough and presses the interact key.
        if (distance <= interactRange && (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.leftShiftKey.wasPressedThisFrame))
        {
            StartCoroutine(AbsorbOrbs());
        }
    }

    IEnumerator AbsorbOrbs()
    {
        absorbed = true;
        GameObject[] orbs = orbSystem.GetOrbs();

        // Attach an absorb behaviour to every active orb.
        foreach (var orb in orbs)
        {
            if (orb != null)
            {
                var absorb = orb.AddComponent<OrbAbsorb>();
                absorb.AbsorbToTarget(player, absorptionDuration);
            }
        }

        // Wait for all orbs to finish absorbing
        yield return new WaitForSeconds(absorptionDuration + 0.2f);
    }
}