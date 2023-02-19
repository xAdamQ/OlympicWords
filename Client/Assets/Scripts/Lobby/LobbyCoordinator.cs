using Cysharp.Threading.Tasks;
using UnityEngine;

public class LobbyCoordinator : MonoModule<LobbyCoordinator>
{
    public Transform Canvas;

    //todo, how the money aid will work now!!! it  can be instant with limit!! makes sense
    public void AddMoney(int amount)
    {
        AddMoneyPopup.Show(amount)
            .Forget(e => throw e);
    }

    public void Logout()
    {
        if (!FbManager.Logout())
            if (PlayerPrefs.HasKey("GuestGuid"))
            {
                PlayerPrefs.DeleteKey("GuestGuid");
                PlayerPrefs.Save();
            }
        //if not logged in with fb, logout from guest

        NetManager.I.RestartGame();
    }
}