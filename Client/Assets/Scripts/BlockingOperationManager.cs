using System;
using Cysharp.Threading.Tasks;

public class BlockingOperationManager : Singleton<BlockingOperationManager>
{
    /// <summary>
    /// invoke, block, and forget
    /// </summary>
    public void Forget(UniTask operation, Action onComplete = null)
    {
        Start(operation).Forget(e => throw e);
    }

    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public async UniTask Start(UniTask operation)
    {
        await BlockingPanel.Show();
        try
        {
            await operation;
            BlockingPanel.Hide();
        }
        catch (BadUserInputException) //todo test if you can get bad user input exc here
        {
            BlockingPanel.Done("operation is not allowed");
            throw;
        }
    }

    public void Forget<T>(UniTask<T> operation, Action<T> onComplete)
    {
        Start(operation).ContinueWith(onComplete)
            .Forget(e => throw e); //the error exception happens normally inside start
    }

    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public async UniTask<T> Start<T>(UniTask<T> operation)
    {
        await BlockingPanel.Show();
        try
        {
            var result = await operation;
            BlockingPanel.Hide();
            return result;
        }
        catch (BadUserInputException) //todo test if you can get bad user input exc here
        {
            BlockingPanel.Done("operation is not allowed");
            throw;
        }
    }
}