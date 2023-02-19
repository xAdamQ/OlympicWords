public class GraphJumpItemPlayer : GraphItemPlayer<GraphJumpEnv>
{
}

public abstract class GraphJumpItemPlayer<TGraphJumpEnv> : GraphItemPlayer<TGraphJumpEnv>
    where TGraphJumpEnv : GraphJumpEnv
{
}