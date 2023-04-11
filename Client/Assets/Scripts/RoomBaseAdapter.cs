using TMPro;
using UnityEngine;

public class RoomBaseAdapter : MonoModule<RoomBaseAdapter>
{
    public TMP_Text ReadyText;
    public GameObject PowerUpPanel, WaitingPanel;

    public void Surrender()
    {
        RootEnv.I.Surrender();
    }
}