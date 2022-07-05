using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public interface IToast
{
    /// <param name="seconds"> auto hide after seconds, -1 disable auto hide</param>
    void Show(string message, float seconds = 1);
    void Hide();
}

public class ConsoleToast : IToast
{
    public void Show(string message, float seconds)
    {
        Debug.Log($"toast showed with message {message ?? ""}");
    }

    public void Hide()
    {
        Debug.Log("toast hide");
    }
}

public class Toast : MonoBehaviour, IToast
{
    public static IToast I;

    public static async UniTask Create()
    {
        I = (await Addressables.InstantiateAsync("toast", Controller.I.canvas)).GetComponent<Toast>();
    }

    [SerializeField] private TMP_Text messageText;

    private CancellationTokenSource currentMessageTS;

    public void Show(string message, float seconds = 1)
    {
        currentMessageTS?.Cancel();

        messageText.text = message ?? "";

        var milliSeconds = (int) (seconds * 1000);

        currentMessageTS = new CancellationTokenSource();

        UniTask.Delay(milliSeconds, cancellationToken: currentMessageTS.Token).ContinueWith(Hide).Forget();

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        messageText.text = "";
        gameObject.SetActive(false);
    }
}