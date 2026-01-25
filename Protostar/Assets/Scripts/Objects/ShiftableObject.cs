using UnityEngine;

public class ShiftableObject : MonoBehaviour, IFocusable, IEngageable, IShiftable
{

    [SerializeField] private ShiftState[] states;
    private int _stateIndex = 0;
    private bool _engaged;

    public void Shift(int direction)
    {
        int newIndex = (_stateIndex + direction) % states.Length;
    }

    private void DisableStates()
    {
        foreach (var state in states)
        {
            state.StateRoot.SetActive(false);
        }
    }

    public void ChangeState(int index)
    {
        states[_stateIndex].StateRoot.SetActive(false);
        states[index].StateRoot.SetActive(true);
        _stateIndex = index;
    }

    void Start()
    {
        DisableStates();
        ChangeState(0);
    }

    public void Focus(GameObject interactor)
    {
        Debug.Log("Focused on object " + this.gameObject.name);
    }

    public void Unfocus(GameObject interactor)
    {
        Debug.Log("Unfocused on object " + this.gameObject.name);
    }

    public void Engage(GameObject interactor)
    {
        _engaged = true;
        Debug.Log("Engaged with " + this.gameObject.name);
    }

    public void Disengage(GameObject interactor)
    {
        _engaged = false;
        Debug.Log("Disengaged with " + this.gameObject.name);
    }
}
