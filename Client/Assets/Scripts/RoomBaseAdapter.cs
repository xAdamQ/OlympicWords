using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomBaseAdapter : MonoModule<RoomBaseAdapter>
{
    public TMP_Text ReadyText;
    public GameObject PowerUpPanel, WaitingPanel;

    public void Surrender()
    {
        UniTask.Create(async () =>
        {
            await RoomNet.I.Surrender();
            SceneManager.LoadScene("Lobby");
        }).Forget(e => throw e);
    }
}