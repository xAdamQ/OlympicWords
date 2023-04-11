/// <summary>
/// we can the intermediate character directly, to play with it in more than env
/// </summary>
public class GraphJumpItemPlayer : GraphItemPlayer<GraphJumpEnv>
{
}

/// <summary>
/// or instead we can derive from it for a more specific env
/// </summary>
public abstract class GraphJumpItemPlayer<TGraphJumpEnv> : GraphItemPlayer<TGraphJumpEnv>
    where TGraphJumpEnv : GraphJumpEnv
{
}