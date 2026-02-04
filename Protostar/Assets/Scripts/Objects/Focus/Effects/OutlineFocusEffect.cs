using System.Collections.Generic;
using UnityEngine;

public class OutlineFocusEffect : MonoBehaviour, IFocusEffect
{
    [SerializeField] private GameObject _baseObject;
    [SerializeField] private RenderingLayerMask outlineLayerMask;

    private Renderer[] _renderers;
    private Dictionary<Renderer, RenderingLayerMask> _originalRenderingLayerMask = new Dictionary<Renderer, RenderingLayerMask>();

    public void Start()
    {
        if (_baseObject == null)
        {
            _baseObject = gameObject;
        }
        _renderers = _baseObject.GetComponentsInChildren<Renderer>(true);
    }

    public void OnFocus()
    {
        if (_renderers != null)
        {
            foreach (Renderer renderer in _renderers)
            {
                _originalRenderingLayerMask[renderer] = renderer.renderingLayerMask;
                renderer.renderingLayerMask |= outlineLayerMask;
            }
        }
    }

    public void OnUnfocus()
    {
        if (_renderers != null)
        {
            foreach (Renderer renderer in _renderers)
            {
                renderer.renderingLayerMask = _originalRenderingLayerMask[renderer];
            }
        }
    }
}