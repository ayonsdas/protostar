using TMPro;
using UnityEngine;

public class PickupTooltip : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private TextMeshProUGUI textMesh;

    private const string NO_HELD_TEXT = "Press {Interact} to pick up objects. Objects that can be picked up or used will glow!";
    private const string HELD_TEXT = "Press {Interact} again to put objects down or place them somewhere.";

    private string _currentText;
    public string CurrentText
    {
        get { return _currentText; }
        private set
        {
            _currentText = value;
            if (textMesh != null)
            {
                textMesh.text = ActionBindingUtil.ReplaceBindings(value);
            }
        }
    }

    private void Start()
    {
        CurrentText = NO_HELD_TEXT;
    }

    private void OnEnable()
    {
        interactor.OnCarriedObjectChange += HandleCarriedObjectChange;
        InputModeManager.Instance.InputModeChanged += HandleInputModeChanged;
    }

    private void OnDisable()
    {
        interactor.OnCarriedObjectChange -= HandleCarriedObjectChange;
        InputModeManager.Instance.InputModeChanged -= HandleInputModeChanged;
    }

    private void HandleCarriedObjectChange(GameObject obj)
    {
        if (textMesh == null) return;
        if (obj == null)
        {
            CurrentText = NO_HELD_TEXT;
        }
        else
        {
            CurrentText = HELD_TEXT;
        }
    }

    private void HandleInputModeChanged(InputMode _inputMode)
    {
        // Using this to update text since the setter will replace binding values in the text mesh
        CurrentText = CurrentText;
    }
}
