// using JetBrains.Annotations;
// using System;
// using System.ComponentModel;
// using System.Runtime.CompilerServices;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
//
// public class PersonalFullUserInfoObservable<TInfo> : FullUserInfo<TInfo>, INotifyPropertyChanged
//     where TInfo : Basra.Common.PersonalFullUserInfo
// {
//     public override TInfo Info { get; set; }
//     public PersonalFullUserInfoObservable(TInfo info) : base(info)
//     {
//         Info = info;
//
//         UniTask.Create(async () =>
//         {
//             await UniTask.DelayFrame(1); //i delay to see data in object initializer
//             if (MoneyAimTimeLeft != null && MoneyAimTimeLeft != TimeSpan.Zero)
//                 DecreaseMoneyAimTimeLeft().Forget();
//         });
//     }
//
//     public event PropertyChangedEventHandler PropertyChanged;
//     [NotifyPropertyChangedInvocator]
//     protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
//     {
//         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//     }
//
//     /// <summary>
//     /// I use this rather than timestamp when the request is done
//     /// because datetime.now is not universal for all clients
//     /// even if I sent current server time and made MoneyAimTimeLeft = server time - request time
//     /// it would be the same as sending it directly form the server  
//     /// </summary>
//     public TimeSpan? MoneyAimTimeLeft
//     {
//         get => Info.MoneyAimTimeLeft;
//         set
//         {
//             Info.MoneyAimTimeLeft = value;
//             NotifyPropertyChanged();
//         }
//     }
//
//     public int Money
//     {
//         get => Info.Money;
//         set
//         {
//             Info.Money = value;
//             NotifyPropertyChanged();
//         }
//     }
//
//     public int Level
//     {
//         get => Info.Level;
//         set
//         {
//             Info.Level = value;
//             NotifyPropertyChanged();
//         }
//     }
//
//     public int Xp
//     {
//         get => Info.Xp;
//         set
//         {
//             Info.Xp = value;
//             NotifyPropertyChanged();
//         }
//     }
//
//     private async UniTaskVoid DecreaseMoneyAimTimeLeft()
//     {
//         var updateRate = 1;
//         while (MoneyAimTimeLeft > TimeSpan.Zero)
//         {
//             Debug.Log("info is changing");
//             MoneyAimTimeLeft = MoneyAimTimeLeft.Value.Subtract(TimeSpan.FromSeconds(updateRate));
//             await UniTask.Delay(TimeSpan.FromSeconds(updateRate));
//         }
//
//         MoneyAimTimeLeft = TimeSpan.Zero;
//     }
// }
//
// public class PersonalFullUserInfo : PersonalFullUserInfoObservable<Basra.Common.PersonalFullUserInfo>
// {
//     public PersonalFullUserInfo(Basra.Common.PersonalFullUserInfo info) : base(info)
//     {
//     }
// }

