using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class BlockingPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button dismissButton; //for both cancellation and dismiss
    [SerializeField] private Image waitImage;

    private static BlockingPanel i;

    private static TweenerCore<Quaternion, Vector3, QuaternionOptions> animTween;

    public static async UniTask Show(string message = null, Action dismissButtonAction = null)
    {
        if (i) Destroy(i.gameObject);
        //you can remove this to support multiple panels
        //but the new should draw over the old  

        i = (await Addressables.InstantiateAsync("blockingPanel", Controller.I.canvas))
            .GetComponent<BlockingPanel>();

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

        i.messageText.text = message ?? "";

        animTween = i.waitImage.transform.DORotate(Vector3.forward * 180, 2f)
            .SetLoops(9999, LoopType.Yoyo);
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