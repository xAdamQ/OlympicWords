using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;


public abstract class MonoModule<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T I;

    /// <summary>
    /// Make a public static Create in the module and call this  
    /// </summary>
    protected static async UniTask Create(string address, Transform parent)
    {
        I = (await Addressables.InstantiateAsync(address, parent)).GetComponent<T>();
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
}


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