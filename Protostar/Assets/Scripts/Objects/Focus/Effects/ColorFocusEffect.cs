using UnityEngine;

public class ColorFocusEffect : MonoBehaviour, IFocusEffect
{
    [SerializeField] private GameObject _baseObject;
    [SerializeField] private Color _newColor;

    private Renderer[] _renderers;
    private Color[] _originalColors;

    public void Start()
    {
        _renderers = _baseObject.GetComponentsInChildren<Renderer>(true);
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] = _renderers[i].material.color;
        }
    }

    public void OnFocus()
    {
        if (_renderers != null)
        {
            foreach (Renderer renderer in _renderers)
            {
                renderer.material.color = _newColor;
            }
        }
    }

    public void OnUnfocus()
    {
        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].material.color = _originalColors[i];
            }
        }
    }
}