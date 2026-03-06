using TMPro;
using UnityEngine;

public class ActionBindingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;

    [TextArea]
    [Tooltip("Message to display, using {} enclosing an action name to replace with the current bindings")]
    [SerializeField] private string message;

    private void OnEnable()
    {
        InputModeManager.Instance.InputModeChanged += HandleInputModeChanged;
    }

    private void OnDisable()
    {
        InputModeManager.Instance.InputModeChanged -= HandleInputModeChanged;
    }

    private void Start()
    {
        if (textMesh != null)
        {
            textMesh.text = ActionBindingUtil.ReplaceBindings(message);
        }
    }

    private void HandleInputModeChanged(InputMode _inputMode)
    {
        if (textMesh != null)
        {
            textMesh.text = ActionBindingUtil.ReplaceBindings(message);
        }
    }
}
