using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class RoomBaseAdapter : MonoModule<RoomBaseAdapter>
{
    public TMP_Text ReadyText;
    public GameObject PowerUpPanel, WaitingPanel;
    [FormerlySerializedAs("menuGate")] public GameObject MenuGate;

    public void Surrender()
    {
        RootEnv.I.Surrender();
    }
}