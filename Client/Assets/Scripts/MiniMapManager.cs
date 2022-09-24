using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreLinq.Extensions;
using UnityEngine;

public class MiniMapManager : MonoBehaviour
{
    [SerializeField] private GameObject miniMapPrefab;
    public Sprite firstIcon, normalIcon, lastIcon;
    private readonly List<MiniMap> miniMaps = new();

    private IEnumerator Start()
    {
        yield return new WaitForFixedUpdate();

        foreach (var playerBase in EnvBase.I.Players)
        {
            var miniMap = Instantiate(miniMapPrefab, transform).GetComponent<MiniMap>();
            miniMaps.Add(miniMap);

            miniMap.Init(playerBase);
            playerBase.MovedADigit += () => OnMovedADigit(miniMap, playerBase);
        }
    }

    private void OnMovedADigit(MiniMap miniMap, PlayerBase playerBase)
    {
        SetFill(miniMap, playerBase.globalCharIndex / (float)EnvBase.I.TotalTextLength);
    }

    private void SetFill(MiniMap miniMap, float percent)
    {
        miniMap.SetFill(percent);
        SetIcons();
    }

    private void SetIcons()
    {
        var ordered = miniMaps.OrderByDescending(m => m.slider.value).ToList();

        ordered[0].slideIcon.sprite = firstIcon;
        for (var i = 1; i < ordered.Count - 1; i++)
            ordered[i].slideIcon.sprite = normalIcon;
        ordered[^1].slideIcon.sprite = lastIcon;
    }
}