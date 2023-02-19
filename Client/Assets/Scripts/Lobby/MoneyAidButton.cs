using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MoneyAidButton : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;

    private void Start()
    {
        if (Repository.I != null)
            Repository.I.PersonalFullInfo.PropertyChanged += OnInfoChanged;
    }

    public void UpdateState()
    {
        var info = Repository.I.PersonalFullInfo;

        if (info.MoneyAimTimePassed >= ConstData.MoneyAimTime)
        {
            GetComponent<Image>().color = new Color(0, 1, 0, 1f);
            buttonText.text = $"${RootEnv.MinBet} خد";
        } //claimable
        else if (info.MoneyAimTimePassed != null)
        {
            GetComponent<Image>().color = new Color(1, 1, 1, 1f);

            // var remainingTime = TimeSpan.FromSeconds((ConstData.MoneyAimTime - info.MoneyAimTimePassed).Value);
            var remainingTime2 = (int)(ConstData.MoneyAimTime - info.MoneyAimTimePassed).Value;
            // buttonText.text = $"{RoomController.MinBet} in {remainingTime:mm\\:ss}";
            buttonText.text =
                $"{RootEnv.MinBet} in {remainingTime2 / 60:00}:{remainingTime2 % 60:00}";
        } //pending
        else if (info.Money >= RootEnv.MinBet || info.MoneyAidRequested >= 4)
        {
            GetComponent<Image>().color = new Color(0, 0, 0, .5f);
            buttonText.text = "خلاص";
        } //can't ask
        else
        {
            GetComponent<Image>().color = new Color(1, 1, 0, 1f);
            buttonText.text = $"${RootEnv.MinBet} اطلب";
        } //ask
    }

    //visual state is updated, this has nothing to do with visuals
    public void OnClick()
    {
        HandleAidClick().Forget();
    }

    private async UniTaskVoid HandleAidClick()
    {
        var info = Repository.I.PersonalFullInfo;

        if (info.MoneyAimTimePassed >= ConstData.MoneyAimTime)
        {
            await Controllers.Lobby.ClaimMoneyAid();

            info.MoneyAimTimePassed = null;
            info.Money += RootEnv.MinBet;

            UpdateState();
        } //claimable
        else if (info.MoneyAimTimePassed != null)
        {
            Toast.I.Show(Translatable.GetText("wait_time"));
        } //pending
        else if (info.MoneyAidRequested >= 4)
        {
            Toast.I.Show(Translatable.GetText("daily_limit"));
        } //max requests reached
        else if (info.Money >= RootEnv.MinBet) //from here MoneyAimTimeLeft = null for sure
        {
            Toast.I.Show(Translatable.GetText("already_money"));
        } //can't ask, a lot of money
        else
        {
            await Controllers.Lobby.ClaimMoneyAid();

            info.MoneyAimTimePassed = 0;
            info.DecreaseMoneyAimTimeLeft().Forget();
            info.MoneyAidRequested++;

            UpdateState();
        } //ask
    }

    private void OnInfoChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Repository.I.PersonalFullInfo.MoneyAimTimePassed))
            UpdateState();
    }

    private void OnDestroy()
    {
        Repository.I.PersonalFullInfo.PropertyChanged -= OnInfoChanged;
    }
}