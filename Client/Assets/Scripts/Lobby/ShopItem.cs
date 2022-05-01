using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    /// <summary>
    /// identifier for the item
    /// </summary>
    private int id;
    public ShopItemState State;
    private int price;

    [SerializeField] private ShopItemStateView stateView;
    [SerializeField] private Image cardbackImage;

    public void Init(ShopItemState shopItemState, int price, Sprite sprite, int index)
    {
        this.id = index;
        cardbackImage.sprite = sprite;
        this.price = price;

        SetState(shopItemState);
    }

    public void SetState(ShopItemState shopItemState)
    {
        State = shopItemState;

        switch (shopItemState)
        {
            case ShopItemState.Locked:
                stateView.Background.color = Color.red;
                stateView.Desc.text = price + "$";
                break;
            case ShopItemState.Unlocked:
                stateView.Background.color = Color.green;
                stateView.Desc.text = "available";
                break;
            case ShopItemState.Set:
                stateView.Background.color = Color.yellow;
                stateView.Desc.text = "used";
                break;
        }
    }

    /// <summary>
    /// uses BlockingOperationManager
    /// </summary>
    public void OnClick()
    {
        switch (State)
        {
            case ShopItemState.Locked:
                BlockingOperationManager.I.Forget(TryUnlock());
                break;
            case ShopItemState.Unlocked:
                BlockingOperationManager.I.Forget(TrySet());
                break;
        }
    }

    /// <summary>
    /// uses Repository, Toast, Controller
    /// this require server assertion
    /// </summary>
    private async UniTask TryUnlock()
    {
        //client assertion
        if (State != ShopItemState.Locked)
        {
            Debug.LogError(
                $"cardback with index {id} is already unlocked or set, what the fuck you're doing");
            return;
        }
        if (Repository.I.PersonalFullInfo.Money < price)
        {
            Toast.I.Show(Translatable.GetText("no_money"), 2);
            return;
        }

        await Controller.I.BuyItem(Shop.Active.ItemType, id);
        //stop interaction? it depends on whether interacting will create issues or not.
        //and usually it will
        //await feedback
        //(1) block all interactions
        //(2) block this item only
        //unless you have a mechanism for blocking individual items easily(which is not true)
        //for example closing and reopening the panel will discard the blocking!

        //if I am here, the server assured

        if (Shop.Active.ItemType == ItemType.Cardback)
            Repository.I.PersonalFullInfo.OwnedCardBackIds.Add(id);
        else
            Repository.I.PersonalFullInfo.OwnedBackgroundsIds.Add(id);

        Repository.I.PersonalFullInfo.Money -= price; //propagate visually

        SetState(ShopItemState.Unlocked); //change the visuals also
    }

    /// <summary>
    /// uses Controller
    /// </summary>
    private async UniTask TrySet()
    {
        switch (State)
        {
            //instant client validation
            case ShopItemState.Locked:
                Debug.LogError($"cardback with index {id} is issued set while it's locked");
                return;
            case ShopItemState.Set:
                return;
        }

        await Controller.I.SelectItem(Shop.Active.ItemType, id);

        if (Shop.Active.ItemType == ItemType.Cardback)
            Repository.I.PersonalFullInfo.SelectedCardback = id;
        else
            Repository.I.PersonalFullInfo.SelectedBackground = id;

        Shop.Active.UnselectedCurrentCard();
        SetState(ShopItemState.Set);
    }
}