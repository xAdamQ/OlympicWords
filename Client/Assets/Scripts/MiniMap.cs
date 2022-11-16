using System;
using System.Collections.Generic;
using MoreLinq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniMap : MonoBehaviour
{
    public Slider slider;
    public Image slideIcon;
    [SerializeField] private TMP_Text playerName;

    public void SetFill(float percent)
    {
        slider.value = percent;
        //todo there is room for performance improvement here
    }

    public void Init(PlayerBase playerBase)
    {
        playerName.text = EnvBase.I.UserInfos[playerBase.Index].Name;
    }
}