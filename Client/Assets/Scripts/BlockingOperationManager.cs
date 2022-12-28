using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class BlockingOperationManager
{
    /// <summary>
    /// invoke, block, and forget
    /// </summary>
    public static void Forget(UniTask operation, Action onComplete = null)
    {
        Start(operation).Forget(e => throw e);
    }
    /// <summary>
    /// invoke, block, and forget
    /// </summary>
    public static void Forget(Task operation, Action onComplete = null)
    {
        Start(operation).Forget(e => throw e);
    }
    public static void Forget<T>(Task<T> operation, Action<T> onComplete)
    {
        Start(operation).ContinueWith(onComplete)
            .Forget(e => throw e); //the error exception happens normally inside start
    }
    public static void Forget<T>(UniTask<T> operation, Action<T> onComplete)
    {
        Start(operation).ContinueWith(onComplete)
            .Forget(e => throw e); //the error exception happens normally inside start
    }

    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public static async UniTask Start(Task operation)
    {
        BlockingPanel.Show();

        try
        {
            await operation;
        }
        //todo test if you can get bad user input exc here
        catch (BadUserInputException)
        {
            Toast.I.Show("operation is not allowed");
            throw;
        }
        catch (Exception)
        {
            Toast.I.Show("something wrong happened");
            throw;
        }
        finally
        {
            BlockingPanel.Hide();
        }
    }
    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public static async UniTask Start(UniTask operation)
    {
        BlockingPanel.Show();
        try
        {
            await operation;
        }
        catch (BadUserInputException)
        {
            Toast.I.Show("operation is not allowed");
            throw;
        }
        catch (Exception)
        {
            Toast.I.Show("something wrong happened");
            throw;
        }
        finally
        {
            BlockingPanel.Hide();
        }
    }
    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public static async UniTask Start(AsyncOperationHandle operation, string message = null)
    {
        BlockingPanel.Show(operation, message);
        try
        {
            await operation;
        }
        catch (BadUserInputException)
        {
            Toast.I.Show("operation is not allowed");
            throw;
        }
        catch (Exception)
        {
            Toast.I.Show("something wrong happened");
            throw;
        }
        finally
        {
            BlockingPanel.Hide();
        }
    }
    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public static async UniTask<T> Start<T>(UniTask<T> operation)
    {
        BlockingPanel.Show();
        try
        {
            var result = await operation;
            return result;
        }
        catch (BadUserInputException)
        {
            Toast.I.Show("operation is not allowed");
            throw;
        }
        catch (Exception)
        {
            Toast.I.Show("something wrong happened");
            throw;
        }
        finally
        {
            BlockingPanel.Hide();
        }
    }
    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public static async UniTask<T> Start<T>(Task<T> operation)
    {
        BlockingPanel.Show();
        try
        {
            var result = await operation;
            return result;
        }
        catch (BadUserInputException)
        {
            Toast.I.Show("operation is not allowed");
            throw;
        }
        catch (Exception)
        {
            Toast.I.Show("something wrong happened");
            throw;
        }
        finally
        {
            BlockingPanel.Hide();
        }
    }
}