using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;


public abstract class MonoModule<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T I;

    /// <summary>
    /// Make a public static Create in the module and call this  
    /// </summary>
    // protected static async UniTask Create(string address, Transform parent)
    // {
    //     I = (await Addressables.InstantiateAsync(address, parent)).GetComponent<T>();
    // }
    private static GameObject prefab;

    private static Transform parent;

    public static void SetSource(GameObject prefab, Transform parent)
    {
        MonoModule<T>.prefab = prefab;
        MonoModule<T>.parent = parent;
    }

    protected virtual void Awake()
    {
        I = gameObject.GetComponent<T>(); //because "this" won't work
    }

    public void Destroy()
    {
        Destroy(I.gameObject);
        I = null;
    }

    public static void DestroyModule()
    {
        if (!I) return;

        Object.Destroy(I.gameObject);
        I = null;
    }
}

// public abstract class SoloMonoModule<T> : MonoModule<T> where T : MonoBehaviour
// {
//     protected override void Awake()
//     {
//         if (I) Destroy(this);
//         base.Awake();
//     }
// }


public interface IGameObject
{
    GameObject GameObject { get; }
}

public abstract class MonoModule2<T> : MonoBehaviour where T : IGameObject
{
    public static T I;

    protected virtual void Awake()
    {
        I = gameObject.GetComponent<T>(); //because "this" won't work
    }
}