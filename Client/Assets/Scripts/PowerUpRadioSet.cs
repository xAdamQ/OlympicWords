using Cysharp.Threading.Tasks;

public class PowerUpRadioSet : RadioSet
{
    protected override void Choose(int choice)
    {
        UniTask.Create(async () =>
        {
            await RoomNet.I.SetPowerUp(choice);
            base.Choose(choice);
        });
    }
}