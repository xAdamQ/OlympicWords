using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum ItemType
{
    Cardback,
    Background,
}

public enum CardbackType
{
    red,
    green,
    blue,

    redRect,
    greenRect,
    blueRect,

    redStar,
    greenStar,
    blueStar,

    redStarPlus,
    greenStarPlus,
    blueStarPlus,
}

public enum BackgroundType
{
    blackFlower,
    berry,
    brownLeaf,
    blueTrans,
    arabesqueBlack,
    veges,
    greenLines,
    hearts,
    eyes,
    purpleCris
}

/// <summary>
/// the difference between this and msg panel is here we create the shop dynamically
/// this is better in case you didn't open the shop at all, no need to load sprites in memory
/// however, index identifying is not good for future edits
/// solutions:
/// 1- set address for each item sprite representing its id, or even a name
/// but first I need to make sure that serialized assets is not loaded by default
/// ordering by name?! 
/// 
/// </summary>
public class Shop : MonoBehaviour
{
    [SerializeField] private GameObject shopItemPrefab;

    [SerializeField] private Transform layoutGroup;
    [SerializeField] private GameObject shopPanel;

    // [SerializeField] private string[] itemSpriteAddresses;
    //you could use list of asset references

    /*
    to add an item, what to do?
    
    1- add it's price to the server
    2- add it's string adress to the client and append this address in the enum as last element
     */

    private static CardbackType[] cardbacksInOrder =
    {
        CardbackType.blue,
        CardbackType.red,
        CardbackType.green,

        CardbackType.blueRect,
        CardbackType.redRect,
        CardbackType.greenRect,

        CardbackType.blueStar,
        CardbackType.redStar,
        CardbackType.greenStar,

        CardbackType.blueStarPlus,
        CardbackType.redStarPlus,
        CardbackType.greenStarPlus,
    };

    private static BackgroundType[] backgroundInOrder =
    {
        BackgroundType.blackFlower,
        BackgroundType.berry,
        BackgroundType.brownLeaf,
        BackgroundType.blueTrans,
        BackgroundType.arabesqueBlack,
        BackgroundType.eyes,
        BackgroundType.purpleCris,
        BackgroundType.veges,
        BackgroundType.greenLines,
        BackgroundType.hearts,
    };

    private readonly List<ShopItem> shopItems = new List<ShopItem>();

    public ItemType ItemType { get; private set; }

    public static async UniTask Create(Transform parent, ItemType itemType)
    {
        (await Addressables.InstantiateAsync(
                itemType == ItemType.Cardback ? "cardbackShop" : "backgroundShop", parent))
            .GetComponent<Shop>().ItemType = itemType;
    }

    public static Shop Active;

    /// <summary>
    /// uses BlockingOperationManager
    /// </summary>
    public async void ShowPanel()
    {
        if (Active) Active.HidePanel();
        Active = this;

        await BlockingOperationManager.I.Start(LoadItems());
        shopPanel.SetActive(true);
    }
    public void HidePanel()
    {
        Active = null;
        shopPanel.SetActive(false);
        foreach (Transform t in layoutGroup) Destroy(t.gameObject); //optional
        shopItems.Clear();
    }

    // private Sprite[] itemSprites;

    /// <summary>
    /// uses Repository
    /// </summary>
    private async UniTask LoadItems()
    {
        // if (itemSprites == null)
        // Debug.Log("item sprites are unloaded");

        // itemSprites ??=
        // ItemType == ItemType.Cardback
        // ? await Addressables.LoadAssetAsync<Sprite[]>("cardbackSprites")
        // : (await Addressables.LoadAssetsAsync<Sprite>("background", _ => { })).ToArray();

        // var itemsInOrder = ItemType == ItemType.Cardback
        //     ? cardbacksOrder.Select(item => item.ToString())
        //     : backgroundOrder.Select(item => item.ToString());

        var spriteAddresses = ItemType == ItemType.Cardback
            ? cardbacksInOrder.Select(_ => _.ToString()).ToList()
            : backgroundInOrder.Select(_ => _.ToString()).ToList();

        //key and direct location(IResourceLocation)
        //api uses the key to search addressables and find the key
        // var loadHandle = Addressables.LoadAssetsAsync<Sprite>(spriteAddresses, _ => { }, Addressables.MergeMode.Union);

        // var itemSprites = await loadHandle;

        var itemType = (int)ItemType;

        for (int i = 0; i < ConstData.ItemPrices[itemType].Length; i++)
        {
            var itemId = ItemType == ItemType.Cardback
                ? (int)cardbacksInOrder[i]
                : (int)backgroundInOrder[i];

            var bought = Repository.I.PersonalFullInfo.OwnedItemIds[itemType].Contains(itemId);
            var inUse = Repository.I.PersonalFullInfo.SelectedItem[itemType] == itemId;

            var state = ShopItemState.Locked;
            if (bought) state = ShopItemState.Unlocked;
            if (inUse) state = ShopItemState.Set;

            //this could happen by create pattern also, but I use create pattern for modules only
            var item = Instantiate(shopItemPrefab, layoutGroup).GetComponent<ShopItem>();

            // var itemId = ConvertItemAddressStringToInt(itemSpriteAddresses[i]);

            await Extensions.LoadAndReleaseAsset<Sprite>(spriteAddresses[i],
                sprite => item.Init(state, ConstData.ItemPrices[itemType][i], sprite, itemId));
            // .Forget(e => throw e);

            // item.Init(state, Repository.ItemPrices[itemType][i], itemSprites[i], itemId);

            shopItems.Add(item);
        }

        // Addressables.Release(loadHandle);
    }

    async UniTask<(T, AsyncOperationHandle<T>)> LoadAssetAsync<T>(string key)
    {
        var loadHandle = Addressables.LoadAssetAsync<T>(key);

        await loadHandle;

        return (loadHandle.Result, loadHandle);
    }

    async UniTask<(IList<T>, AsyncOperationHandle<IList<T>>)> LoadAssetsAsync<T>(string key)
    {
        var loadHandle = Addressables.LoadAssetsAsync<T>(key, _ => { });

        await loadHandle;

        return (loadHandle.Result, loadHandle);
    }


    private IEnumerator LoadItems2()
    {
        //Load a Material
        var locationHandle = Addressables.InstantiateAsync("");
        yield return locationHandle;
        Addressables.Release(locationHandle);
    }
    //
    // private int ConvertItemAddressStringToInt(string address)
    // {x
    //     if (ItemType == ItemType.Cardback)
    //     {
    //         Enum.TryParse(address, out CardbackType cardbackType);
    //         return (int) cardbackType;
    //     }
    //     else
    //     {
    //         Enum.TryParse(address, out BackgroundType backgroundType);
    //         return (int) backgroundType;
    //     }
    // }


    public void UnselectedCurrentCard()
    {
        shopItems.First(c => c.State == ShopItemState.Set).SetState(ShopItemState.Unlocked);
    }
}