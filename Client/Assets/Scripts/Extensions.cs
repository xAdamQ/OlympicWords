using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

public static partial class Extensions
{
    public static T CutRandom<T>(this List<T> list)
    {
        var randIndex = Random.Range(0, list.Count);
        list.RemoveAt(randIndex);
        return list[randIndex];
    }

    public static T GetRandom<T>(this List<T> list)
    {
        if (list.Count == 0)
            throw new System.Exception("you are trying to get a random element from an empty list");

        var randIndex = Random.Range(0, list.Count);
        return list[randIndex];
    }

    public static void AddMultiple<T>(this List<T> list, params T[] args)
    {
        list.AddRange(args);
    }

    public static void ForEach<T>(this T[] array, System.Action<T> action)
    {
        foreach (var item in array) action(item);
    }

    public static async UniTask LoadAndReleaseAsset<T>(string key, System.Action<T> onComplete)
    {
        var handle = Addressables.LoadAssetAsync<T>(key);

        await handle;

        onComplete(handle.Result);
        Addressables.Release(handle);
    }

    public static Vector3 SetY(this Vector3 vector3, float y)
    {
        return new Vector3(vector3.x, y, vector3.z);
    }

    public static string DescriptorString(this object obj)
    {
        var res = new StringBuilder();
        foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
        {
            string name = descriptor.Name;
            object value = descriptor.GetValue(obj);
            res.Append(name + " <> " + value);
        }

        return res.ToString();
    }

    public static Type[] GetParameterTypes(this MethodInfo methodInfo)
    {
        var info = methodInfo.GetParameters();
        var types = new Type[info.Length];
        for (var i = 0; i < types.Length; i++)
        {
            types[i] = info[i].ParameterType;
        }

        return types;
    }

    public static async UniTask InvokeAsync(this MethodInfo method, object obj,
        params object[] parameters)
    {
        await (UniTask) method.Invoke(obj, parameters);
    }

    public static Vector3 DivideBy(this Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
    }

    public static void SkipTween(this Tweener tweener)
    {
        if (tweener is {active: true})
            tweener.Goto(1);
    }
}