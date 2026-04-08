using System.Collections.Generic;
using UnityEngine;

public class CameraOcclusionFade : MonoBehaviour
{
    [Header("Occlusion Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private LayerMask fadeLayers;
    [SerializeField] private float sphereRadius = 0.3f;

    [Header("Dither Settings")]
    [SerializeField] private int ditherSize = 2;
    [SerializeField] private float fadeSpeed = 4f;
    [SerializeField] private float fadeAlpha = 0.2f;

    private HashSet<Renderer> currentHits = new();
    private DitherController ditherController;

    private void Awake()
    {
        ditherController = new DitherController(
            ditherSize: ditherSize,
            fadeSpeed: fadeSpeed,
            fadeAlpha: fadeAlpha
        );
    }

    void LateUpdate()
    {
        currentHits.Clear();

        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        // Find all objects between the camera and the target using a sphere cast
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            sphereRadius,
            direction.normalized,
            distance,
            fadeLayers
        );

        foreach (RaycastHit hit in hits)
        {
            var root = hit.collider.GetComponentInParent<DitherRoot>()?.gameObject ?? hit.collider.gameObject;
            // Get all child renderers of the hit object
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

            foreach (Renderer r in renderers)
            {
                if (r == null)
                    continue;

                // Add the renderer to the current hits set
                currentHits.Add(r);
            }
        }

        // Update the dithering controller with the current hits
        ditherController.UpdateDither(currentHits);
    }
}