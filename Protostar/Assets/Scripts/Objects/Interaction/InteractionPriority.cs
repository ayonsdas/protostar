using System.Collections.Generic;

public static class InteractionPriority
{
    private static readonly Dictionary<InteractionType, int> priorities = new()
    {
        { InteractionType.Shift, 1000 },
        { InteractionType.SlotPlace, 900 },
        { InteractionType.SlotRemove, 850 },
        { InteractionType.Pickup, 800 },
        { InteractionType.Drop, 700 },
        { InteractionType.Engage, 600 },
        { InteractionType.Interact, 500 },
        { InteractionType.None, 0 }
    };

    public static int Get(InteractionType type)
        => priorities[type];
}