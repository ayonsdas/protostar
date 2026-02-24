using System.Collections.Generic;
using System.Linq;

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
