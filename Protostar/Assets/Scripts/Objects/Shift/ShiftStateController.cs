using UnityEngine;

public class ShiftStateController : MonoBehaviour
{

    private MeshRenderer[] _meshRenderers;
    private Collider[] _colliders;
    private ShiftCapability[] _capabilities;

    void Start()
    {
        _meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _capabilities = GetComponentsInChildren<ShiftCapability>(true);
    }

    public void SetActive(bool active)
    {
        foreach (var meshRenderer in _meshRenderers)
        {
            meshRenderer.enabled = active;
        }

        foreach (var collider in _colliders)
        {
            collider.enabled = active;
        }

        foreach (var capability in _capabilities)
        {
            if (active)
            {
                capability.Enable();
            }

            else
            {
                capability.Disable();
            }
        }
    }
}
