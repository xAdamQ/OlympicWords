using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

/// <summary>
/// the shop doesn't need to be env object or have an a generic env args because it's dependent on room requester
/// the relation between the room room requester and the env is 1 to 1, but the shop is 1 to many
/// the generic env arg is left just in case we had a use of it in the  future
/// </summary>
public abstract class Shop : MonoModule<Shop>
{
    private Transform itemsParent;
    private readonly List<AsyncOperationHandle<GameObject>> handles = new();

    public string EnvName;
    public void Init(string envName)
    {
        EnvName = envName;
        GameObject.Find("RoomChoices").transform.GetChild(0).gameObject.SetActive(false);
        itemsParent = GetComponentInChildren<LayoutGroup>().transform;
        StartCoroutine(LoadItemsAsync());
    }

    public Item SelectedItem;

    private IEnumerator LoadItemsAsync()
    {
        foreach (var itemPlayerLocation in AddressManager.I.GetItemPlayerLocationsRecursive(EnvName))
        {
            var handle = Addressables.InstantiateAsync(itemPlayerLocation, itemsParent);
            handles.Add(handle);
            yield return handle;
            handle.Result.GetComponent<Item>().Init(EnvName);
            //I don't use LoadAssetsAsync because I don't want to handle dependency issues later
        }
    }

    public void Close()
    {
        GameObject.Find("RoomChoices").transform.GetChild(0).gameObject.SetActive(true);

        foreach (var asyncOperationHandle in handles)
            Addressables.ReleaseInstance(asyncOperationHandle);

        Addressables.ReleaseInstance(gameObject);
    }
}

public class ClientEnvironment
{
    public string Name { get; set; }
    public List<ClientEnvironment> Children { get; set; } = new();
    public ClientEnvironment Parent { get; set; }
}

public abstract class Shop<TEnv> : Shop
    where TEnv : RootEnv
{
}