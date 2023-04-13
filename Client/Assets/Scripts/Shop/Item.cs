using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ShopItemState
{
    Locked,
    Unlocked,
    Selected
}

public abstract class Item : MonoBehaviour
{
    //instance specific info
    private ShopItemState state;
    private string envName;

    #region config and mapper
    //readonly not change the config
    // public ItemConfig Config;
    // private string Id => Config.Id;
    // private int Price => Config.Price;
    // private int Level => Config.Level;

    //prefab specific info
    [ReadOnly] public string Id;
    public int Price;
    public int Level;

    private void OnValidate()
    {
        SetId();
    }

    //create read only properties from this mapper
    [HideInInspector] public ItemMapper Mapper;
    private TMP_Text PriceText => Mapper.PriceText;
    private TMP_Text LevelText => Mapper.LevelText;
    private GameObject LevelView => Mapper.LevelView;
    private GameObject UsedView => Mapper.UsedView;
    private Button PriceButton => Mapper.PriceButton;
    private Button UseButton => Mapper.UseButton;
    #endregion

    private ShopItemState GetState()
    {
        if (Repository.I.PersonalFullInfo.SelectedItemPlayer[envName] == Id)
            return ShopItemState.Selected;

        if (Repository.I.PersonalFullInfo.OwnedItemPlayers.Contains(Id))
            return ShopItemState.Unlocked;

        return ShopItemState.Locked;
    }

    public void Init(string envName)
    {
        this.envName = envName;

        SetState(GetState());
    }

    private void SetId()
    {
        Id = name;
    }

    private void Awake()
    {
        Mapper = GetComponent<ItemMapper>();

        UseButton.onClick.AddListener(() => TrySelect().Forget());
        PriceButton.onClick.AddListener(() => TryUnlock().Forget());
    }

    public void SetState(ShopItemState shopItemState)
    {
        state = shopItemState;

        UsedView.SetActive(false);
        LevelView.SetActive(false);
        UseButton.gameObject.SetActive(false);
        PriceButton.gameObject.SetActive(false);

        switch (shopItemState)
        {
            case ShopItemState.Locked:
                PriceText.text = Price.ToString();
                PriceButton.gameObject.SetActive(true);
                break;

            case ShopItemState.Unlocked:
                UseButton.gameObject.SetActive(true);
                break;

            case ShopItemState.Selected:

                if (Shop.I.SelectedItem)
                    Shop.I.SelectedItem.SetState(ShopItemState.Unlocked);

                Shop.I.SelectedItem = this;
                UsedView.SetActive(true);
                break;
        }

        //level lock appear only when needed
        if (Repository.I.PersonalFullInfo.CalcLevel() < Level)
        {
            LevelText.text = Level.ToString();
            LevelView.SetActive(true);
        }

        if (Repository.I.PersonalFullInfo.Money < Price)
        {
            PriceButton.interactable = false;
        }
    }

    private async UniTask TryUnlock()
    {
        //client assertion
        if (state != ShopItemState.Locked)
        {
            Debug.LogError
                ($"item with index {Id} is already unlocked or set, dev level issue");
            return;
        }

        if (Repository.I.PersonalFullInfo.Money < Price)
        {
            Debug.LogError
                ($"item with index {Id} is already unlocked or set, dev level issue, or money was changed externally");
            Toast.I.Show(Translatable.GetText("no_money"));
            return;
        }

        await BlockingOperationManager.Start(Controllers.Lobby.BuyPlayer(Id));

        // if I am here, the server said ok

        Repository.I.PersonalFullInfo.OwnedItemPlayers.Add(Id);
        Repository.I.PersonalFullInfo.Money -= Price; //propagate visually

        SetState(ShopItemState.Unlocked); //change the visuals also
    }

    private async UniTask TrySelect()
    {
        switch (state)
        {
            case ShopItemState.Locked:
                Debug.LogError($"item with id {Id} issued set while it's locked");
                return;
            case ShopItemState.Selected:
                Debug.LogError($"item with id {Id} issued set while it's already set");
                return;
        }

        await BlockingOperationManager.Start(Controllers.Lobby.SelectPlayer(Id, envName));
        Repository.I.PersonalFullInfo.SelectedItemPlayer[envName] = Id;

        SetState(ShopItemState.Selected);
    }


    private string GetAbstractName()
    {
        var stringType = GetType().ToString();
        var itemIndex = stringType.IndexOf("Item", StringComparison.Ordinal);
        return stringType[..itemIndex];
    }
}