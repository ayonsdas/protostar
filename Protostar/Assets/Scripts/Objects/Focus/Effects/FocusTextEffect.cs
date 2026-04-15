using TMPro;
using UnityEngine;

[RequireComponent(typeof(ActionBindingText))]
public class FocusTextEffect : MonoBehaviour, IFocusEffect
{
    [SerializeField] private FocusUI focusUI;
    private ActionBindingText _text;

    public void Start()
    {
        _text = GetComponent<ActionBindingText>();

        var textMesh = focusUI.canvas.GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh)
        {
            _text.TextMesh = textMesh;
        }
        else
        {
            Debug.LogError("FocusUI canvas is missing a TextMeshProUGUI component in its children.");
        }

        if (_text)
        {
            _text.enabled = false;
        }
        else
        {
            Debug.LogError("FocusTextEffect is missing reference to ActionBindingText");
        }
    }
    public void OnFocus()
    {
        Debug.Log($"[FocusTextEffect] {gameObject.name} focused, showing {focusUI.name} UI");
        focusUI.ShowUI();
        _text.enabled = true;
    }

    public void OnUnfocus()
    {
        focusUI.HideUI();
        _text.enabled = false;
    }
}
