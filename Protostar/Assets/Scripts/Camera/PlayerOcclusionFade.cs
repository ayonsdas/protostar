using System.Collections.Generic;
using UnityEngine;

public class PlayerOcclusionFade : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private float triggerDistance = 3f;
    [Header("Dither Settings")]
    [SerializeField] private int ditherSize = 5;
    [SerializeField] private float fadeSpeed = 4f;
    [SerializeField] private float fadeAlpha = 0.08f;

    private DitherController ditherController;

    private void Awake()
    {
        ditherController = new DitherController(
            ditherSize: ditherSize,
            fadeSpeed: fadeSpeed,
            fadeAlpha: fadeAlpha
        );
    }

    private void LateUpdate()
    {
        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;
        // If the player is within the trigger distance, add their renderer to the dither controller
        if (distance < triggerDistance)
        {
            ditherController.UpdateDither(new HashSet<Renderer> { playerRenderer });
        }
        // If nothing is hit, clear the dither controller
        else
        {
            ditherController.UpdateDither(new HashSet<Renderer>());
        }
    }
}
