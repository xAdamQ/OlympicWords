using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        UniTask.Create(async () =>
        {
            await MasterHub.I.Surrender();
            SceneManager.LoadScene("Lobby");
        });
    }
}