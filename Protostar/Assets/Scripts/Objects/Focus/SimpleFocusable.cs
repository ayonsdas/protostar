using UnityEngine;

public class SimpleFocusable : MonoBehaviour, IFocusable
{
    private IFocusEffect[] _focusEffects;

    public void Start()
    {
        _focusEffects = gameObject.GetComponents<IFocusEffect>();
    }
    public void Focus(GameObject interactor)
    {
        if (_focusEffects == null) return;
        foreach (IFocusEffect focusEffect in _focusEffects)
        {
            focusEffect.OnFocus();
        }
    }

    public void Unfocus(GameObject interactor)
    {
        if (_focusEffects == null) return;
        foreach (IFocusEffect focusEffect in _focusEffects)
        {
            focusEffect.OnUnfocus();
        }
    }
}