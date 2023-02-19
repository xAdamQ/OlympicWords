using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

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
        I = Instantiate(Coordinator.I.References.ToastPrefab, Coordinator.I.canvas).GetComponent<Toast>();
    }

    [SerializeField] private TMP_Text messageText;

    private CancellationTokenSource currentMessageTs;

    public void Show(string message, float seconds = 1)
    {
        currentMessageTs?.Cancel();

        messageText.text = message ?? "";

        var milliSeconds = (int)(seconds * 1000);

        currentMessageTs = new CancellationTokenSource();

        UniTask.Delay(milliSeconds, cancellationToken: currentMessageTs.Token).ContinueWith(Hide).Forget();

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        messageText.text = "";
        gameObject.SetActive(false);
    }
}