using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

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
    /// invoke, block, and forget
    /// </summary>
    public void Forget(Task operation, Action onComplete = null)
    {
        Start(operation).Forget(e => throw e);
    }
    public void Forget<T>(Task<T> operation, Action<T> onComplete)
    {
        Start(operation).ContinueWith(onComplete)
            .Forget(e => throw e); //the error exception happens normally inside start
    }
    public void Forget<T>(UniTask<T> operation, Action<T> onComplete)
    {
        Start(operation).ContinueWith(onComplete)
            .Forget(e => throw e); //the error exception happens normally inside start
    }

    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public async UniTask Start(Task operation)
    {
        BlockingPanel.Show();
        try
        {
            await operation;
            BlockingPanel.Hide();
        }
        catch (BadUserInputException) //todo test if you can get bad user input exc here
        {
            BlockingPanel.Hide();
            Toast.I.Show("operation is not allowed");
            throw;
        }
        catch (Exception)
        {
            BlockingPanel.Hide();
            Toast.I.Show("something wrong happened");
            throw;
        }
    }
    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public async UniTask Start(UniTask operation)
    {
        BlockingPanel.Show();
        try
        {
            await operation;
            BlockingPanel.Hide();
        }
        catch (BadUserInputException) //todo test if you can get bad user input exc here
        {
            BlockingPanel.Hide();
            Toast.I.Show("operation is not allowed");
            throw;
        }
        catch (Exception)
        {
            BlockingPanel.Hide();
            Toast.I.Show("something wrong happened");
            throw;
        }
    }
    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public async UniTask Start(AsyncOperationHandle operation, string message = null)
    {
        BlockingPanel.Show(operation, message);
        try
        {
            await operation;
            BlockingPanel.Hide();
        }
        catch (BadUserInputException) //todo test if you can get bad user input exc here
        {
            BlockingPanel.Hide();
            Toast.I.Show("operation is not allowed");
            throw;
        }
        catch (Exception)
        {
            BlockingPanel.Hide();
            Toast.I.Show("something wrong happened");
            throw;
        }
    }
    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public async UniTask<T> Start<T>(UniTask<T> operation)
    {
        BlockingPanel.Show();
        try
        {
            var result = await operation;
            BlockingPanel.Hide();
            return result;
        }
        catch (BadUserInputException) //todo test if you can get bad user input exc here
        {
            BlockingPanel.Hide();
            Toast.I.Show("operation is not allowed");
            throw;
        }
        catch (Exception)
        {
            BlockingPanel.Hide();
            Toast.I.Show("something wrong happened");
            throw;
        }
    }
    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public async UniTask<T> Start<T>(Task<T> operation)
    {
        BlockingPanel.Show();
        try
        {
            var result = await operation;
            BlockingPanel.Hide();
            return result;
        }
        catch (BadUserInputException) //todo test if you can get bad user input exc here
        {
            BlockingPanel.Hide();
            Toast.I.Show("operation is not allowed");
            throw;
        }
        catch (Exception)
        {
            BlockingPanel.Hide();
            Toast.I.Show("something wrong happened");
            throw;
        }
    }
}