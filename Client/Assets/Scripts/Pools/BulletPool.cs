using System;

public class BulletPool : Pool<BulletPool>
{
    protected override void Awake()
    {
        base.Awake();

        RootEnv.Initiated += OnEnvInitiated;
    }
    private void OnEnvInitiated()
    {
        if (RootEnv.I is GraphShootEnv)
            Init();
        else
            Clear();
    }

    private void OnDestroy()
    {
        RootEnv.Initiated -= OnEnvInitiated;
    }
}