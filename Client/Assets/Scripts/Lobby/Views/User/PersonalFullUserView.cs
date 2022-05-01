//using System.ComponentModel;
//using UnityEngine;
//using UnityEngine.UI;
//using Zenject;

///// <summary>
///// only single instance possible at a time
///// </summary>
//public class PersonalFullUserView : FullUserView
//{
//    [SerializeField] private Text money;

//    public void Show(PersonalFullUserInfo personalFullInfo)
//    {
//        base.Show(personalFullInfo);
//        money.text = personalFullInfo.Money.ToString();
//    }
//}