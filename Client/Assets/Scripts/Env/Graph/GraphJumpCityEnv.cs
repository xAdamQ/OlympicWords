using System;
public class GraphJumpCityEnv : GraphJumpEnv
{
    public override Type GetControllerType()
    {
        return typeof(GraphJumpCityController);
    }
}