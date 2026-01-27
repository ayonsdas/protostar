using System.Collections.Generic;
using UnityEngine;

public class OutlineFocusEffect : MonoBehaviour, IFocusEffect
{
    [SerializeField] private GameObject _baseObject;
    [SerializeField] private Material _outlineMaterial;

    private Renderer[] _renderers;
    private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();

    public void Start()
    {
        _renderers = _baseObject.GetComponentsInChildren<Renderer>(true);
    }

    public void OnFocus()
    {
        if (_renderers != null)
        {
            foreach (Renderer renderer in _renderers)
            {
                List<Material> currentMaterials = new List<Material>();
                renderer.GetSharedMaterials(currentMaterials);
                _originalMaterials[renderer] = currentMaterials.ToArray();

                currentMaterials.Add(_outlineMaterial);
                renderer.SetSharedMaterials(currentMaterials);
            }
        }
    }

    public void OnUnfocus()
    {
        if (_renderers != null)
        {
            foreach (Renderer renderer in _renderers)
            {
                renderer.sharedMaterials = _originalMaterials[renderer];
            }
        }
    }
}