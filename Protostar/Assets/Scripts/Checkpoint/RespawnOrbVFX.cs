using UnityEngine;
using System.Collections;

public class RespawnOrbVFX : MonoBehaviour
{
    [Header("References")]
    public Transform spawnTransform;
    public GameObject orbVFXPrefab;

    [Header("Absorption Settings")]
    public float absorptionDuration = 0.8f;
    private GameObject vfxInstance;
    private OrbVFX orbSystem;


    public void SpawnOrbs()
    {
        vfxInstance = Instantiate(orbVFXPrefab, spawnTransform.position, spawnTransform.rotation);
        orbSystem = vfxInstance.GetComponent<OrbVFX>();
    }

    public void AbsorbOrbs()
    {
        StartCoroutine(AbsorbRoutine());
    }

    IEnumerator AbsorbRoutine()
    {
        // Wait a frame for OrbVFX.Start() to populate the orbs array
        yield return null;

        GameObject[] orbs = orbSystem.GetOrbs();
        foreach (var orb in orbs)
        {
            if (orb != null)
            {
                var absorb = orb.AddComponent<OrbAbsorb>();
                absorb.AbsorbToTarget(spawnTransform, absorptionDuration);
            }
        }

        yield return new WaitForSeconds(absorptionDuration + 0.2f);
        Destroy(vfxInstance);
    }
}