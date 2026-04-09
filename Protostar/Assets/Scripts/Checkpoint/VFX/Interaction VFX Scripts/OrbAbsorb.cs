using UnityEngine;
using System.Collections;


public class OrbAbsorb : MonoBehaviour
{
    public void AbsorbToTarget(Transform target, float duration)
    {
        StartCoroutine(Absorb(target, duration));
    }

    IEnumerator Absorb(Transform target, float duration)
    {
        // Unparent so the orb moves in world space independently of the item.
        transform.SetParent(null);

        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Lerp position toward a point slightly above the target's pivot.
            transform.position = Vector3.Lerp(startPos, target.position + Vector3.up * .25f, t);

            // Shrink from full scale to zero over the same duration.
            transform.localScale = startScale * Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        Destroy(gameObject);
    }
}