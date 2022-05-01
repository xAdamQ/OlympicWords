#if HMS
using Cysharp.Threading.Tasks;
using HmsPlugin;
using HuaweiMobileServices.IAP;

public class IapShop : MonoModule<IapShop>
{
    public static void Create()
    {
        Create("iapShop", LobbyController.I.Canvas)
            .Forget(e => throw e);
    }

    private void Start()
    {
        HMSIAPManager.Instance.OnBuyProductSuccess += OnPurchaseSuccess;
    }

    private void OnDestroy()
    {
        HMSIAPManager.Instance.OnBuyProductSuccess -= OnPurchaseSuccess;
    }

    private void OnPurchaseSuccess(PurchaseResultInfo obj)
    {
        Controller.I.Send("MakePurchase", obj.InAppPurchaseDataRawJSON, obj.InAppDataSignature);
    }

    public void MakePurchase(string productId)
    {
        HMSIAPManager.Instance.BuyProduct(productId);
    }
}
#endif