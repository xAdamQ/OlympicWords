using Cysharp.Threading.Tasks;
using UnityEngine;

public class ChallengeResponsePanel : MonoBehaviour
{
    [SerializeField] private MinUserView minUserView;

    public static void Show(MinUserInfo senderInfo)
    {
        // UniTask.Create(async () =>
        // {
        //     var panel = (await Addressables.InstantiateAsync("challengeResponsePanel",
        //         LobbyController.I.Canvas)).GetComponent<ChallengeResponsePanel>();
        //
        //     panel.minUserView.Init(senderInfo);
        // });
    }

    public void Respond(int response)
    {
        UniTask.Create(async () =>
        {
            var res = await Controllers.Lobby
                .RespondChallengeRequest(minUserView.MinUserInfo.Id, response == 0);

            Toast.I.Show(res.ToString());

            Destroy(gameObject);
        });
    }
}