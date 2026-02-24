using UnityEngine;
using Game.Objects.Shift;
using System.Collections.Generic;

public class ShiftableObject : MonoBehaviour, IEngageable, IShiftable, IInteractionCandidate
{

    [SerializeField] private float shiftCooldown = 0.25f;
    [SerializeField] private ShiftState[] states;

    private float _lastShiftTime;
    private int _stateIndex = 0;
    private bool _engaged;

    void Start()
    {
        DisableStates();
        ChangeState(0);
    }

    public void Shift(int direction)
    {

        if (Time.time < _lastShiftTime + shiftCooldown) return;
        _lastShiftTime = Time.time;

        if (!_engaged) return;

        int newIndex = (_stateIndex + direction) % states.Length;

        // Needed to fix negative modulo since -1 % 5 = -1 in C#
        newIndex = (newIndex + states.Length) % states.Length;
        ChangeState(newIndex);
        Debug.Log("Shifted to " + newIndex);
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

    public void Engage(GameObject interactor)
    {
        _engaged = true;
        Debug.Log("[ShiftableObject] Engaged with " + gameObject.name);
    }

    public void Disengage(GameObject interactor)
    {
        _engaged = false;
        Debug.Log("[ShiftableObject] Disengaged with " + gameObject.name);
    }

    public void CollectOptions(PlayerInteractionContext context, List<InteractionOption> options)
    {
        options.Add(InteractionBuilder.Create(
            InteractionType.Shift,
            this
        ));
    }
}
