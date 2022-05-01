using Cysharp.Threading.Tasks;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// uses Repository.I.PersonalFullInfo, 
/// </summary>
public class PersonalActiveUserView : MinUserView
{
    public static async UniTask Create()
    {
        await Addressables.InstantiateAsync("personalActiveUserView", LobbyController.I.Canvas);
    }

    [SerializeField] private TMP_Text money;

    [SerializeField] private MoneyAidButton moneyAidButton;

    private void Start()
    {
        Repository.I.PersonalFullInfo.PropertyChanged += OnInfoChanged;

        Init(Repository.I.PersonalFullInfo);
        Money = Repository.I.PersonalFullInfo.Money;
        Xp = Repository.I.PersonalFullInfo.Xp;

        moneyAidButton.UpdateState();
    }

    private void OnDestroy()
    {
        Repository.I.PersonalFullInfo.PropertyChanged -= OnInfoChanged;
    }

    public override void ShowFullInfo()
    {
        FullUserView.Show(Repository.I.PersonalFullInfo);
    }

    protected virtual void OnInfoChanged(object sender, PropertyChangedEventArgs e)
    {
        var info = Repository.I.PersonalFullInfo;
        switch (e.PropertyName)
        {
            case nameof(info.SelectedTitleId):
                Title = PlayerBase.Titles[info.SelectedTitleId];
                break;
            case nameof(info.Money):
                Money = info.Money;
                break;
            case nameof(info.Xp):
                Xp = info.Xp;
                break;
        }
    }

    [SerializeField] private TMP_Text xpText;
    [SerializeField] private RectTransform fill, fillBackground;

    private const float Expo = .55f, Divi = 10;
    public static int GetStartXpOfLevel(int level)
    {
        if (level == 0) return 0;
        return (int)Mathf.Pow(10, Mathf.Log10(Divi * level) / Expo);
    }

    public int Xp
    {
        set
        {
            var myLevel = Repository.I.PersonalFullInfo.CalcLevel();
            var startXpOfNextLevel = GetStartXpOfLevel(myLevel + 1);
            var startXpOfCurrentLevel = GetStartXpOfLevel(myLevel);
            var finishPercent = (float)(value - startXpOfCurrentLevel) / (startXpOfNextLevel - startXpOfCurrentLevel);

            xpText.text = $"{value}/{startXpOfNextLevel}";
            fill.sizeDelta = new Vector2(finishPercent * fillBackground.sizeDelta.x, fill.sizeDelta.y);
        }
    }

    public int Money
    {
        set => money.text = value.ToString();
    }

    public void testwaitalot()
    {
        BlockingOperationManager.I.Forget(Controller.I.SendAsync("TestWaitAlot"));
        // _blockingOperationManager.Forget<MinUserInfo>(_controller.TestWaitWithReturn(), info => Debug.Log("info is" + info.Name));
    }
}