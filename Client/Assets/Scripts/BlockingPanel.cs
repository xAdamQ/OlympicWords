using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class BlockingPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button dismissButton; //for both cancellation and dismiss
    [SerializeField] private Image waitImage;
    [SerializeField] private FillBar fillBar;

    private static BlockingPanel i;

    private static TweenerCore<Quaternion, Vector3, QuaternionOptions> animTween;

#if ADDRESSABLES
    public static void Show(AsyncOperationHandle handle, string message = null)
    {
        ShowBase(message);
        i.fillBar.SetHandle(handle);

        i.waitImage.gameObject.SetActive(false);
        i.fillBar.gameObject.SetActive(true);
    }
#endif

    public static void Show(string message = null, Action dismissButtonAction = null)
    {
        ShowBase(message);

        if (dismissButtonAction != null)
        {
            i.dismissButton.onClick.AddListener(() => dismissButtonAction());
            //if you want to reuse the same object make sure to save and remove this
            i.dismissButton.gameObject.SetActive(true);
        }
        else
        {
            i.dismissButton.gameObject.SetActive(false);
        }

        i.waitImage.gameObject.SetActive(true);
        i.fillBar.gameObject.SetActive(false);

        animTween = i.waitImage.transform.DORotate(Vector3.forward * 180, 2f)
            .SetLoops(9999, LoopType.Yoyo);
    }

    private static void ShowBase(string message = "")
    {
        if (i) Destroy(i.gameObject);

        i = Instantiate(Coordinator.I.References.BlockingPanelPrefab, Coordinator.I.canvas)
            .GetComponent<BlockingPanel>();

        i.messageText.text = message;
    }

    public static void HideDismiss()
    {
        i.dismissButton.gameObject.SetActive(false);
    }

    public static void Done(string message)
    {
        animTween.Kill();
        i.messageText.text = message;
        i.dismissButton.gameObject.SetActive(true);
    }

    //you shouldn't hide manually if you have cancellation action
    //cancel itself hide
    public static void Hide()
    {
        if (i) //this line enables you to call hide aggressively
            Destroy(i.gameObject);
    }
}