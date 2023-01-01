using Cysharp.Threading.Tasks;
using UnityEngine;

public class PowerUpRadioSet : RadioSet
{
    protected override void Choose(int choice)
    {
        UniTask.Create(async () =>
        {
            await RoomNet.I.SetPowerUp(choice);
            Debug.Log("set power up done");
            base.Choose(choice);
        });
    }
}