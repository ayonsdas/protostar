using UnityEngine;
using System.Collections;


/// Handles player interaction with an orb collectible.
/// When the player is within range and presses F, all orbiting orbs
/// are absorbed toward the player and destroyed.
public class InteractableOrbItem : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Maximum distance the player can be from this item to interact (press F).")]
    public float interactRange = 3f;


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
        if (distance <= interactRange && Input.GetKeyDown(KeyCode.F))
        {
            absorbed = true;
            StartCoroutine(orbSystem.AbsorbOrbs());
        }
    }
}