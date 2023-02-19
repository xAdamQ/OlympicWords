using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shared.Controllers;

// ReSharper disable once ClassNeverInstantiated.Global
public class ControllerProxy<T> : DispatchProxyAsync where T : class, IController
{
    private string address;

    public ControllerProxy()
    {
        SetAddress();
    }

    private void SetAddress()
    {
        var typeName = typeof(T).Name;
        var controllerIndex = typeName.IndexOf("Controller", StringComparison.Ordinal);
        var controllerName = typeName[1..controllerIndex];

        address = NetManager.I.SelectedAddress + '/' + controllerName + '/';
    }

    public override object Invoke(MethodInfo method, object[] args)
    {
        throw new NotImplementedException();
    }

    public override async Task InvokeAsync(MethodInfo method, object[] args)
    {
        await NetManager.I.SendAsyncHTTP(address + method.Name, GetQueryParams(method, args));
    }

    public override async Task<TResult> InvokeAsyncT<TResult>(MethodInfo method, object[] args)
    {
        return await NetManager.I.GetAsync<TResult>(address + method.Name, GetQueryParams(method, args));
    }

    private static List<(string key, string value)> GetQueryParams(MethodInfo method, IReadOnlyList<object> args)
    {
        var qParams = new List<(string key, string value)>(args.Count);
        var methodParams = method.GetParameters();
        for (var i = 0; i < args.Count; i++)
            qParams[i] = (methodParams[i].Name, args[i].ToString());

        return qParams;
    }


    public static T CreateProxy()
    {
        return Create<T, ControllerProxy<T>>();
    }
}