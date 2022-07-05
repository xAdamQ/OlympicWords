using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ChallengeResponsePanel : MonoBehaviour
{
    public enum ChallengeResponseResult
    {
        Offline, //player is offline whatever the response
        Canceled, //player is not interested anymore
        Success, //successful whatever the response
    }

    [SerializeField] private MinUserView minUserView;

    public static void Show(MinUserInfo senderInfo)
    {
        UniTask.Create(async () =>
        {
            var panel = (await Addressables.InstantiateAsync("challengeResponsePanel",
                LobbyController.I.Canvas)).GetComponent<ChallengeResponsePanel>();

            panel.minUserView.Init(senderInfo);
        });
    }

    public void Respond(int response)
    {
        UniTask.Create(async () =>
        {
            var res = await NetManager.I.InvokeAsync<ChallengeResponseResult>(
                "RespondChallengeRequest", minUserView.MinUserInfo.Id, response == 0);

            Toast.I.Show(res.ToString());

            Destroy(gameObject);
        });
    }
}