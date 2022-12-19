using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using Wintellect.PowerCollections;
using Random = UnityEngine.Random;
using SystemRandom = System.Random;

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
            throw new Exception("you are trying to get a random element from an empty list");

        var randIndex = Random.Range(0, list.Count);
        return list[randIndex];
    }

    public static T GetRandom<T>(this List<T> list, SystemRandom random)
    {
        if (list.Count == 0)
            throw new Exception("you are trying to get a random element from an empty list");

        var randIndex = random.Next(list.Count);
        return list[randIndex];
    }

    public static void AddMultiple<T>(this List<T> list, params T[] args)
    {
        list.AddRange(args);
    }

    public static void ForEach<T>(this IEnumerable<T> array, Action<T> action)
    {
        foreach (var item in array) action(item);
    }

    public static async UniTask LoadAndReleaseAsset<T>(string key, Action<T> onComplete)
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
            var name = descriptor.Name;
            var value = descriptor.GetValue(obj);
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
        await (UniTask)method.Invoke(obj, parameters);
    }

    public static Vector3 DivideBy(this Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
    }

    public static void SkipTween(this Tweener tweener)
    {
        if (tweener is { active: true })
            tweener.Goto(1);
    }

    public static List<List<T>> EmptyList<T>(int count)
    {
        var res = new List<List<T>>();
        for (var i = 0; i < count; i++)
            res.Add(new List<T>());

        return res;
    }

    public static Vector3 Sum(this IEnumerable<Vector3> vector3s)
    {
        var res = new Vector3();
        foreach (var vector3 in vector3s)
        {
            res.x += vector3.x;
            res.y += vector3.y;
            res.z += vector3.z;
        }

        return res;
    }

    public static Vector2 Sum(this IEnumerable<Vector2> vector2s)
    {
        var res = new Vector2();
        foreach (var vector3 in vector2s)
        {
            res.x += vector3.x;
            res.y += vector3.y;
        }

        return res;
    }

    /// <summary>
    /// tuple elements must be of the same type T, since you pass T yourself, it can be a base type for different element types
    /// if tuple has value types they will be boxed and unboxed
    /// </summary>
    public static IEnumerable<T> ToEnumerable<T>(this ITuple tuple)
    {
        for (var i = 0; i < tuple.Length; i++)
            yield return (T)tuple[i];
    }

    public static TValue GetAdjacent<TKey, TValue>(this SortedList<TKey, TValue> sortedList,
        TKey element, int offset)
    {
        var currIndex = sortedList.IndexOfKey(element);

        var adjKey = sortedList.Keys[currIndex + offset];
        var adj = sortedList[adjKey];

        return adj;
    }

    public static KeyValuePair<T, T> DirectUpperAndLower<T>(this OrderedSet<T> set, T NewItem)
    {
        var exists = set.Add(NewItem);

        var myIndex = set.IndexOf(NewItem);

        var v = myIndex - 1 < 0 ? default : set[myIndex - 1];
        var k = myIndex + 1 > set.Count - 1 ? default : set[myIndex + 1];

        if (!exists) set.Remove(NewItem);

        return new KeyValuePair<T, T>(k, v);
    }

    public static Vector2 TakeXZ(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }

    public static Vector3 XYInXZ(this Vector2 vector)
    {
        return new Vector3(vector.x, 0, vector.y);
    }

    public static int Sign(this float number)
    {
        return number >= 0 ? 1 : -1;
    }

    public static string UriCombine(params string[] uriParts)
    {
        var uri = string.Empty;
        if (uriParts != null && uriParts.Any())
        {
            var trims = new[] { '\\', '/' };
            uri = (uriParts[0] ?? string.Empty).TrimEnd(trims);

            for (var i = 1; i < uriParts.Length; i++)
                uri = $"{uri.TrimEnd(trims)}/{(uriParts[i] ?? string.Empty).TrimStart(trims)}";
        }

        return uri;
    }


    public static async UniTask<Texture2D> GetRemoteTexture(string url)
    {
        using var request = UnityWebRequestTexture.GetTexture(url);

#if UNITY_EDITOR
        request.certificateHandler = new CertificateWhore();
#endif

        //if there wer an exception here, just throw it
        await request.SendWebRequest();

        if (request.result is UnityWebRequest.Result.ConnectionError
            or UnityWebRequest.Result.ProtocolError)
        {
            // log error:
            Debug.Log($"{request.error}, URL:{request.url}");

            // nothing to return on error:
            return null;
        }

        // Debug.Log($"img downloaded from: {url}");
        return DownloadHandlerTexture.GetContent(request);
    }

    public static IEnumerator GetRemoteTexture(string url, Action<Texture2D> callback)
    {
        using var request = UnityWebRequestTexture.GetTexture(url);

#if UNITY_EDITOR
        request.certificateHandler = new CertificateWhore();
#endif

        //if there wer an exception here, just throw it
        yield return request.SendWebRequest();

        if (request.result is UnityWebRequest.Result.ConnectionError
            or UnityWebRequest.Result.ProtocolError)
        {
            // log error:
            Debug.Log($"{request.error}, URL:{request.url}");

            // nothing to return on error:
            yield break;
        }

        var texture = DownloadHandlerTexture.GetContent(request);
        callback?.Invoke(texture);
    }
}

public class CertificateWhore : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}