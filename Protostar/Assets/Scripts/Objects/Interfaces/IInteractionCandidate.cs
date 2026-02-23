using System.Collections.Generic;

public interface IInteractionCandidate
{
    void CollectOptions(PlayerInteractionContext context, List<InteractionOption> options);
}
