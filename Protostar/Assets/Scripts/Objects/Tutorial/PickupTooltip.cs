using TMPro;
using UnityEngine;

public class PickupTooltip : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private TextMeshProUGUI textMesh;

    private const string NO_HELD_TEXT = "Press F to pick up objects. Objects that can be picked up or used will glow!";
    private const string HELD_TEXT = "Press F again to put place held objects.";

    private void Start()
    {
        if(textMesh != null) textMesh.text = NO_HELD_TEXT;
    }

    private void OnEnable()
    {
        interactor.OnCarriedObjectChange += OnCarriedObjectChange;
    }

    private void OnDisable()
    {
        interactor.OnCarriedObjectChange += OnCarriedObjectChange;
    }

    private void OnCarriedObjectChange(GameObject obj)
    {
        if (textMesh == null) return;
        if (obj == null)
        {
            textMesh.text = NO_HELD_TEXT;
        }
        else
        {
            textMesh.text = HELD_TEXT;
        }
    }
}
