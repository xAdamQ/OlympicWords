using System;
using System.Threading.Tasks;
using UnityEngine;

public abstract class ControllerBase
{
    private string Address => NetManager.I.SelectedAddress + '/' + controllerName + '/';
    private string controllerName;

    protected ControllerBase()
    {
        SetAddress();
    }

    private void SetAddress()
    {
        var typeName = GetType().Name;
        Debug.Log($"type name was {typeName}");
        var controllerIndex = typeName.IndexOf("Controller", StringComparison.Ordinal);
        var controllerName = typeName[..controllerIndex];

        this.controllerName = controllerName;
    }

    protected Task<T> GetAsync<T>(string methodName, params (string name, string value)[] queryParams)
    {
        return NetManager.I.GetAsync<T>(Address + methodName, queryParams);
    }

    protected Task SendAsync(string methodName, params (string name, string value)[] queryParams)
    {
        return NetManager.I.SendAsyncHTTP(Address + methodName, queryParams);
    }
}