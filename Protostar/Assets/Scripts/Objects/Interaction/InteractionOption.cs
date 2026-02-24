using System;
using System.Collections.Generic;
using UnityEngine;

public struct InteractionOption
{
    public InteractionType Type;
    // UI message to display
    public string Prompt;
    // What to do on interact
    public Action<PlayerInteractor> OnPressed;
    public Action<PlayerInteractor> OnReleased;
    public MonoBehaviour Source;
    public InteractionInputType InputType;

    public bool IsValid => ValidType && ValidAction;
    private bool ValidType => Type != InteractionType.None;
    private bool ValidAction => OnPressed != null || OnReleased != null;
}

public struct InteractionDefinition
{
    public InteractionInputType InputType;
    public InteractionType Type;
    public string DefaultPrompt;
    public Action<PlayerInteractor, MonoBehaviour> DefaultOnPressed;
    public Action<PlayerInteractor, MonoBehaviour> DefaultOnReleased;
}

public static class InteractionBuilder
{
    private static readonly Dictionary<InteractionType, InteractionDefinition> _definitions
        = new Dictionary<InteractionType, InteractionDefinition>
    {
        {
            InteractionType.Shift,
            new InteractionDefinition
            {
                Type = InteractionType.Shift,
                InputType = InteractionInputType.Shift,
                DefaultPrompt = "",
                DefaultOnPressed = (interactor, source) =>
                {
                    interactor.TryBeginShift(source);
                },
                DefaultOnReleased = (interactor, source) =>
                {
                    interactor.EndShift();
                }
            }
        },
        {

            InteractionType.SlotPlace,
            new InteractionDefinition
            {
                Type = InteractionType.SlotPlace,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = "",
                DefaultOnPressed = (interactor, source) =>
                {
                    interactor.TryPlaceInto(source);
                },
                DefaultOnReleased = (interactor, source) => { }
            }
        },
        {
            InteractionType.SlotRemove,
            new InteractionDefinition
            {
                Type = InteractionType.SlotRemove,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = "",
                DefaultOnPressed = (interactor, source) =>
                {
                    interactor.TryTakeFrom(source);
                },
                DefaultOnReleased = (interactor, source) => { }
            }
        },
        {
            InteractionType.Pickup,
            new InteractionDefinition
            {
                Type = InteractionType.Pickup,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = "",
                DefaultOnPressed = (interactor, source) =>
                {
                    interactor.PickupObject(source);
                },
                DefaultOnReleased = (interactor, source) => { }
            }
        },
        {
            InteractionType.Drop,
            new InteractionDefinition
            {
                Type = InteractionType.Drop,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = "",
                DefaultOnPressed = (interactor, source) =>
                {
                    interactor.DropCarriedObject();
                },
                DefaultOnReleased = (interactor, source) => { }
            }
        },
        // TODO Implement this if we actually plan to have any engagable, non-shiftable objects  
        {
            InteractionType.Engage,
            new InteractionDefinition
            {
                Type = InteractionType.Engage,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = "",
                DefaultOnPressed = (interactor, source) => { },
                DefaultOnReleased = (interactor, source) => { }
            }
        },
        {
            InteractionType.Interact,
            new InteractionDefinition
            {
                Type = InteractionType.Interact,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = "",
                DefaultOnPressed = (interactor, source) =>
                {
                    interactor.InteractWithObject(source);
                },
                DefaultOnReleased = (interactor, source) => { }
            }
        },
        {
            InteractionType.Inspect,
            new InteractionDefinition
            {
                Type = InteractionType.Inspect,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = "",
                DefaultOnPressed = (interactor, source) => { },
                DefaultOnReleased = (interactor, source) => { }
            }
        }
    };

    public static InteractionOption Create(
        InteractionType type,
        MonoBehaviour source,
        string promptOverride = null)
    {
        var def = _definitions[type];

        return new InteractionOption
        {
            Type = type,
            InputType = def.InputType,
            Source = source,
            Prompt = promptOverride ?? def.DefaultPrompt,
            OnPressed = interactor => def.DefaultOnPressed(interactor, source),
            OnReleased = interactor => def.DefaultOnReleased(interactor, source)
        };
    }
}