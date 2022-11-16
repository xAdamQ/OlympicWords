using TMPro;
using UnityEngine;

public class RoomBaseAdapter : MonoModule<RoomBaseAdapter>
{
    public TMP_Text ReadyText;
    public GameObject PowerUpPanel;

    public void SetPowerUp(int powerUp)
    {
        MasterHub.I.SetPowerUp(powerUp);
    }

    public void Surrender()
    {
        MasterHub.I.Surrender();
    }
}