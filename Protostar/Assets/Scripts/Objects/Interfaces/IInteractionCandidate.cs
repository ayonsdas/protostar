using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Interface needed to be implemented for any interaction to occur with the player
/// Specifies InteractionOptions that the object provides. Usually implemented alongside
/// other suppporting interfaces, e.g. IShiftable, for an InteractionOption of type Shift
/// </summary>
public interface IInteractionCandidate
{
    void CollectOptions(PlayerInteractionContext context, List<InteractionOption> options);
    bool HasAnyValidInteraction(PlayerInteractionContext context)
    {
        List<InteractionOption> tempOptions = new();
        CollectOptions(context, tempOptions);

        return tempOptions.Any(o => o.IsValid);
    }
}
