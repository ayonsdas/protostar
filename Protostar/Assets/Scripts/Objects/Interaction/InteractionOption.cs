using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Struct used in interaction for interactable objects to provide capabilities
/// Includes type inputType, and callbacks for press and release
/// </summary>
public struct InteractionOption
{
    public InteractionType Type;
    public InteractionInputType InputType;
    // UI message to display
    public string Prompt;
    // What to do when InputType is Pressed / Released
    public Action<PlayerInteractor> OnPressed;
    public Action<PlayerInteractor> OnReleased;
    // Monobehaviour providing capabilities, to be used for focus, pickup, etc
    public MonoBehaviour Source;


    public bool IsValid => ValidType && ValidAction;
    private bool ValidType => Type != InteractionType.None;
    private bool ValidAction => OnPressed != null || OnReleased != null;
}


/// <summary>
/// Builder class to more easily create standard InteractionOptions by Type
/// Defines defaults, needing Type, and source to be specified
/// </summary>
public static class InteractionBuilder
{
    /// <summary>
    /// Internal struct to store defaults to be used in InteractionOption construction
    /// </summary>
    private struct InteractionDefinition
    {
        public InteractionInputType InputType;
        public InteractionType Type;
        public string DefaultPrompt;
        public Action<PlayerInteractor, MonoBehaviour> DefaultOnPressed;
        public Action<PlayerInteractor, MonoBehaviour> DefaultOnReleased;
    }

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
                    interactor.TrySlotPlace(source);
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
                    interactor.TrySlotRemove(source);
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
        // TODO Change this if we actually plan to have any engagable, non-shiftable objects  
        {
            InteractionType.Engage,
            new InteractionDefinition
            {
                Type = InteractionType.Engage,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = "",
                DefaultOnPressed = (interactor, source) =>
                {
                    interactor.ToggleEngage(source, lockMovement: true);
                },
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
        // Special type that is used primarily for displaying message, but no real interaction
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

    /// <summary>
    /// Creates instance of InteracitonOption of the given type
    /// </summary>
    /// <param name="type">The InteractionType being used</param>
    /// <param name="type">The MonoBehaviour providing this InteractionOption</param>
    /// <param name="promptOverride">Optional UI prompt to include</param>
    /// <returns>A default InteractionOption of of the given type</returns>
    public static InteractionOption Create(
        InteractionType type,
        MonoBehaviour source,
        string promptOverride = null,
        Action<PlayerInteractor> onPressedOverride = null,
        Action<PlayerInteractor> onReleasedOverride = null
    )
    {
        var def = _definitions[type];

        return new InteractionOption
        {
            Type = type,
            InputType = def.InputType,
            Source = source,
            Prompt = promptOverride ?? def.DefaultPrompt,
            OnPressed = onPressedOverride ?? (interactor => def.DefaultOnPressed(interactor, source)),
            OnReleased = onReleasedOverride ?? (interactor => def.DefaultOnReleased(interactor, source)),
        };
    }
}