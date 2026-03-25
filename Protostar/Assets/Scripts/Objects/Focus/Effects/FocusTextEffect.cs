using TMPro;
using UnityEngine;

public class FocusTextEffect : MonoBehaviour, IFocusEffect
{
    [SerializeField] private FocusUI focusUI;
    [SerializeField] private ActionBindingText text;

    public void Start()
    {
        if (text)
        {
            text.enabled = false;
        }
        else
        {
            Debug.LogError("FocusTextEffect is missing reference to ActionBindingText");
        }
    }
    public void OnFocus()
    {
        focusUI.ShowUI();
        text.enabled = true;
    }

    public void OnUnfocus()
    {
        focusUI.HideUI();
        text.enabled = false;
    }
}
