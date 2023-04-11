using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

public static class BlockingOperationManager
{
    /// <summary>
    /// invoke, block, and forget
    /// </summary>
    public static void Forget(UniTask operation, Action onSuccess = null, string msg = "")
    {
        Start(operation, onSuccess, msg).Forget(e => throw e);
    }
    /// <summary>
    /// invoke, block, and forget
    /// </summary>
    public static void Forget(Task operation, Action onSuccess = null)
    {
        Start(operation, onSuccess).Forget(e => throw e);
    }
    public static void Forget<T>(Task<T> operation, Action<T> onSuccess)
    {
        Start(operation, onSuccess).ContinueWith(onSuccess)
            .Forget(e => throw e); //the error exception happens normally inside start
    }
    public static void Forget<T>(UniTask<T> operation, Action<T> onSuccess)
    {
        Start(operation, onSuccess).ContinueWith(onSuccess)
            .Forget(e => throw e); //the error exception happens normally inside start
    }

    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public static async UniTask Start(Task operation, Action onSuccess = null)
    {
        BlockingPanel.Show();

        try
        {
            await operation;
            onSuccess?.Invoke();
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
    public static async UniTask Start(UniTask operation, Action onSuccess = null, string msg = "")
    {
        BlockingPanel.Show(msg);
        try
        {
            await operation;
            onSuccess?.Invoke();
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
#if ADDRESSABLES
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
#endif

    /// <summary>
    /// uses BlockingPanel 
    /// </summary>
    public static async UniTask<T> Start<T>(UniTask<T> operation, Action<T> onSuccess = null)
    {
        BlockingPanel.Show();
        try
        {
            var result = await operation;
            onSuccess?.Invoke(result);
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
    public static async UniTask<T> Start<T>(Task<T> operation, Action<T> onSuccess = null)
    {
        BlockingPanel.Show();
        try
        {
            var result = await operation;
            onSuccess?.Invoke(result);
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