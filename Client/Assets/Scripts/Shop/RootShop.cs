public abstract class RootShop<TRootEnv> : Shop<TRootEnv> where TRootEnv : RootEnv
{
}

public class RootShop : RootShop<RootEnv>
{
}