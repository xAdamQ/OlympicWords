using System;
public abstract class ItemPlayer : Item
{
}

public abstract class ItemPlayer<TEnv> : ItemPlayer where TEnv : RootEnv
{
    protected override Type GetEnvType()
    {
        return typeof(TEnv);
    }
}