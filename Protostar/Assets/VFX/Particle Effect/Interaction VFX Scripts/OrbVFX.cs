using UnityEngine.VFX;
using UnityEngine;


/// Spawns a ring of orb prefabs and orbits them around this GameObject.
/// The orbs transition between an "idle" and "excited" state based on how
/// close the player is.
public class OrbVFX : MonoBehaviour
{
    private static readonly int GlowIntensityID = Shader.PropertyToID("GlowIntensity");

    [Header("Orb Settings")]
    public GameObject orbPrefab;

    [Tooltip("Number of orbs spawned in the ring.")]
    public int orbCount = 6;

    [Tooltip("Base rotational speed of the orbit.")]
    public float orbitSpeed = 25f;

    [Header("Idle State")]
    [Tooltip("Orbit radius when the player is far away.")]
    public float idleOrbitRadius = 0.4f;
    [Tooltip("Vertical bob speed in idle.")]
    public float idleFloatSpeed = 0.8f;
    [Tooltip("Vertical bob height in idle.")]
    public float idleFloatAmount = 0.15f;

    [Header("Excited State (Player Near)")]
    [Tooltip("Orbit radius when the player is very close.")]
    public float excitedOrbitRadius = 0.6f;
    [Tooltip("Vertical bob speed when excited.")]
    public float excitedFloatSpeed = 2.5f;
    [Tooltip("Vertical bob amplitude when excited.")]
    public float excitedFloatAmount = 0.25f;

    [Header("Proximity")]
    [Tooltip("Distance at which the orbs start reacting to the player.")]
    public float detectionRange = 4f;
    [Tooltip("How quickly the orbs blend between idle and excited states.")]
    public float transitionSpeed = 2f;


    private GameObject[] orbs;
    private float[] angleOffsets;      // Starting angle per orb (evenly spaced)
    private float[] heightOffsets;     // Random phase offset for vertical bob
    private float[] speedVariations;
    private Transform player;
    private float orbTime = 0f;        // Accumulated time used for orbit & bob
    private float proximityFactor = 0f; // 0 = fully idle, 1 = fully excited

    void Start()
    {
        orbs = new GameObject[orbCount];
        angleOffsets = new float[orbCount];
        heightOffsets = new float[orbCount];
        speedVariations = new float[orbCount];

        for (int i = 0; i < orbCount; i++)
        {
            // Spawn each orb as a child so it follows this parent transform.
            orbs[i] = Instantiate(orbPrefab, transform.position, Quaternion.identity);
            orbs[i].transform.SetParent(transform);

            // Distribute orbs evenly around the circle.
            angleOffsets[i] = (360f / orbCount) * i;

            // Randomise height phase & speed so the orbs don't move in lockstep.
            heightOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
            speedVariations[i] = Random.Range(0.8f, 1.2f);
        }

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        UpdateProximity();
        MoveOrbs();
    }

    /// Smoothly ramp between 0 (far) and 1 (close)
    /// based on the player's distance from this object.
    void UpdateProximity()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Map distance -> 0-1 factor (closer = higher).
        float targetFactor = 1f - Mathf.Clamp01(distance / detectionRange);

        // Smoothly interpolate to avoid sudden state jumps.
        proximityFactor = Mathf.Lerp(proximityFactor, targetFactor, Time.deltaTime * transitionSpeed);
    }


    /// Position every orb in a circular orbit and adjust glow intensity
    /// each frame. All values are blended between idle and excited states
    void MoveOrbs()
    {
        // Blend orbit parameters between idle and excited.
        float currentRadius = Mathf.Lerp(idleOrbitRadius, excitedOrbitRadius, proximityFactor);
        float currentSpeed = Mathf.Lerp(idleFloatSpeed, excitedFloatSpeed, proximityFactor);
        float currentFloat = Mathf.Lerp(idleFloatAmount, excitedFloatAmount, proximityFactor);

        orbTime += Time.deltaTime * currentSpeed;

        for (int i = 0; i < orbCount; i++)
        {
            if (orbs[i] == null) continue;

            float time = orbTime * speedVariations[i];
            float angle = angleOffsets[i] + time * orbitSpeed;
            float rad = angle * Mathf.Deg2Rad;

            // Circular orbit on XZ plane + vertical bob.
            float x = Mathf.Cos(rad) * currentRadius;
            float z = Mathf.Sin(rad) * currentRadius;
            float y = Mathf.Sin(time + heightOffsets[i]) * currentFloat;

            orbs[i].transform.localPosition = new Vector3(x, y, z);
        }

        // Adjust VFX Graph glow intensity: dim when idle, bright when excited.
        float glowIntensity = Mathf.Lerp(2f, 12f, proximityFactor);
        foreach (var orb in orbs)
        {
            if (orb == null) continue;
            var vfx = orb.GetComponent<UnityEngine.VFX.VisualEffect>();
            if (vfx != null) vfx.SetFloat(GlowIntensityID, glowIntensity);
        }
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, idleOrbitRadius);    // idle orbit radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, excitedOrbitRadius); // excited orbit radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);     // proximity detection range

        // Also show the interact range if an InteractableOrbItem is attached.
        var item = GetComponent<InteractableOrbItem>();
        if (item != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, item.interactRange);
        }
    }

    public float GetProximityFactor() => proximityFactor;
    public GameObject[] GetOrbs() => orbs;
}