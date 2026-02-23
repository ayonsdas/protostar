using System;
using System.Collections.Generic;
using UnityEngine;

public struct InteractionOption
{
    public InteractionType Type;
    // UI message to display
    public string Prompt;
    // What to do on interact
    public Action Execute;
    public MonoBehaviour Source;
    public InteractionInputType InputType;

    public bool IsValid => Type != InteractionType.None && Execute != null;
}

public struct InteractionDefinition
{
    public InteractionInputType InputType;
    public InteractionType Type;
    public string DefaultPrompt;
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
                DefaultPrompt = ""
            }
        },
        {

            InteractionType.Place,
            new InteractionDefinition
            {
                Type = InteractionType.Place,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = ""
            }
        },
        {
            InteractionType.RemoveFromSlot,
            new InteractionDefinition
            {
                Type = InteractionType.RemoveFromSlot,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = ""
            }
        },
        {
            InteractionType.Pickup,
            new InteractionDefinition
            {
                Type = InteractionType.Pickup,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = ""
            }
        },
        {
            InteractionType.Drop,
            new InteractionDefinition
            {
                Type = InteractionType.Drop,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = ""
            }
        },
        {
            InteractionType.Engage,
            new InteractionDefinition
            {
                Type = InteractionType.Engage,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = ""
            }
        },
        {
            InteractionType.Interact,
            new InteractionDefinition
            {
                Type = InteractionType.Interact,
                InputType = InteractionInputType.Interact,
                DefaultPrompt = ""
            }
        }
    };

    public static InteractionOption Create(
        InteractionType type,
        MonoBehaviour source,
        Action execute,
        string promptOverride = null)
    {
        var def = _definitions[type];

        return new InteractionOption
        {
            Type = type,
            InputType = def.InputType,
            Source = source,
            Prompt = promptOverride ?? def.DefaultPrompt,
            Execute = execute
        };
    }
}