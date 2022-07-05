using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddMoneyPopup : MonoBehaviour
{
    public static async UniTask Show(int money)
    {
        var i = (await Addressables.InstantiateAsync("addMoneyPopup", LobbyController.I.Canvas))
            .GetComponent<AddMoneyPopup>();

        i.moneyText.text = money.ToString();
        Repository.I.PersonalFullInfo.Money += money;
    }

    [SerializeField] private TMP_Text moneyText;

    public void DestroyGo()
    {
        Destroy(gameObject);
    }
}