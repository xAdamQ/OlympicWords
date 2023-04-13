using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class Pool<TPool> : MonoModule<TPool> where TPool : Pool<TPool>
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialSize;
    private readonly Queue<GameObject> available = new();
    private readonly List<GameObject> inUse = new();
    [HideInInspector] public bool Initiated;

    protected override void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(this);
    }

    public void Init()
    {
        if (Initiated) return;

        // if (prefab.GetComponent<IPooledObject>() == null)
        // throw new Exception("Pooled object must implement IPooledObject");

        for (var i = 0; i < initialSize; i++)
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            available.Enqueue(go);
        }

        Initiated = true;
    }

    public void Clear()
    {
        Initiated = false;

        foreach (var go in available) Object.Destroy(go);
        foreach (var go in inUse) Object.Destroy(go);

        available.Clear();
        inUse.Clear();
    }

    public GameObject Take(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var obj = available.Count > 0 ? available.Dequeue() : Instantiate(prefab);

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        if (parent is not null)
            transform.SetParent(parent);
        obj.SetActive(true);
        obj.GetComponent<IPooledObject>()?.Taken();

        inUse.Add(obj);

        return obj;
    }

    public void Release(GameObject go)
    {
        go.SetActive(false);
        transform.SetParent(transform);
        available.Enqueue(go);
        inUse.Remove(go);
    }
}