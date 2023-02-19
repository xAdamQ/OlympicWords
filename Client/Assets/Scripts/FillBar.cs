using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FillBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text text;

    public void SetFill(float percent)
    {
        fill.fillAmount = percent;
        text.text = $"{(int)(percent * 100)}%";
    }

#if ADDRESSABLES
    public void SetHandle(AsyncOperationHandle handle)
    {
        SetFill(0);
        StartCoroutine(SetHandleFill(handle));
    }

    private IEnumerator SetHandleFill(AsyncOperationHandle handle)
    {
        while (!handle.IsDone)
        {
            SetFill(handle.PercentComplete);
            yield return new WaitForFixedUpdate();
        }
    }
#endif
}